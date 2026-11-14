using DongNamTrip.Contracts;
using DongNamTrip.Modules.Administration;
using DongNamTrip.Modules.Itinerary;
using DongNamTrip.Modules.Persistence.Entities;

namespace DongNamTrip.Modules.Quotation;

/// <summary>Nhân viên tiếp nhận, xác minh, báo giá và hủy yêu cầu tour riêng (UC17, UC18); tác vụ hết hạn báo giá.</summary>
public sealed class QuotationAdminService(QuotationRepository repository, IItineraryApi itinerary, IAdministrationApi administration, TimeProvider time,
    ILogger<QuotationAdminService> logger)
{
    private const string RequestType = "YEUCAUTOURRIENG";
    private const string QuoteType = "BAOGIA";
    private const string VerificationType = "XACMINHDICHVU";
    private const string ChangeAction = "Khách đề nghị đổi báo giá";

    private DateTime Now => time.GetLocalNow().DateTime;

    public async Task<PagedResult<QuoteRequestInfo>> ListAsync(QuoteRequestFilter filter, CancellationToken ct)
    {
        var page = Math.Clamp(filter.Page ?? 1, 1, Limits.MaxPage);
        var size = Math.Clamp(filter.Size ?? 20, 1, Limits.MaxPageSize);
        var (rows, total) = await repository.ListAsync(filter, (page - 1) * size, size, ct);
        var items = new List<QuoteRequestInfo>(rows.Count);
        foreach (var row in rows) items.Add(await InfoAsync(row, ct));
        return new PagedResult<QuoteRequestInfo>(items, total, page, size);
    }

    public async Task<AdminQuoteRequestDetail?> GetAsync(int id, CancellationToken ct)
    {
        var row = await repository.RequestAsync(id, ct);
        if (row is null) return null;
        var version = (await itinerary.VersionAsync(row.MaPb, ct))!;
        var quotes = await repository.QuotesAsync(id, ct);
        var verifications = (await repository.VerificationsAsync(id, ct)).Select(ToInfo).ToList();
        var notes = (await administration.HistoryAsync(RequestType, id, ct)).Where(entry => entry.Action == ChangeAction && !string.IsNullOrWhiteSpace(entry.Reason))
            .Select(entry => entry.Reason!).ToList();
        return new AdminQuoteRequestDetail(QuoteMapper.ToInfo(row, QuoteRequestService.CurrentQuote(quotes)?.MaBg), row.LienHe, row.NhuCauDichVu, version,
            await itinerary.CheckVersionAsync(row.MaPb, ct), verifications, quotes.Select(QuoteMapper.ToInfo).ToList(), notes);
    }

    public async Task<OpResult<QuoteRequestInfo>> ReceiveAsync(int id, int actor, CancellationToken ct)
    {
        var row = await repository.RequestAsync(id, ct);
        if (row is null) return Op.NotFound("Không tìm thấy yêu cầu.");
        if (row.TrangThai == QuoteRequestStatus.Verifying) return Op.Ok(await InfoAsync(row, ct));
        if (row.TrangThai != QuoteRequestStatus.Submitted) return Op.Conflict($"Yêu cầu đang ở trạng thái \"{QuoteRequestStatus.Label(row.TrangThai)}\" nên không tiếp nhận được.");
        if (!await repository.MoveRequestAsync(id, QuoteRequestStatus.Submitted, QuoteRequestStatus.Verifying, ct))
            return Op.Conflict("Yêu cầu vừa được cập nhật bởi người khác; vui lòng tải lại.");
        await administration.RecordAsync(new AuditEntry(actor, AuditActions.RequestReceived, RequestType, id, null,
            new { Status = QuoteRequestStatus.Submitted }, new { Status = QuoteRequestStatus.Verifying }), ct);
        return Op.Ok(await InfoAsync((await repository.RequestAsync(id, ct))!, ct));
    }

    public async Task<OpResult<VerificationInfo>> RecordVerificationAsync(int id, VerificationForm form, int actor, CancellationToken ct)
    {
        var item = (form.Item ?? "").Trim();
        var provider = (form.Provider ?? "").Trim();
        var note = string.IsNullOrWhiteSpace(form.Note) ? null : form.Note.Trim();
        if (!VerificationResults.All.Contains(form.Result)) return Op.Invalid("Kết quả xác minh không hợp lệ.");
        if (item.Length is 0 or > 250 || provider.Length is 0 or > 250) return Op.Invalid("Hạng mục và đơn vị cung cấp cần 1–250 ký tự.");
        if (note?.Length > 1000) return Op.Invalid("Ghi chú tối đa 1000 ký tự.");
        if (form.Result is VerificationResults.Unavailable or VerificationResults.Substitute && note is null)
            return Op.Invalid("Hoạt động không đáp ứng hoặc cần thay thế phải có ghi chú đề xuất.");
        var row = await repository.RequestAsync(id, ct);
        if (row is null) return Op.NotFound("Không tìm thấy yêu cầu.");
        if (row.TrangThai != QuoteRequestStatus.Verifying) return Op.Conflict("Yêu cầu chưa được tiếp nhận hoặc đã kết thúc xác minh.");
        var saved = new Xacminhdichvu { MaYc = id, HangMuc = item, DonViCungCap = provider, ThoiDiem = form.At == default ? Now : form.At, KetQua = form.Result, GhiChu = note };
        var verificationId = await repository.AddVerificationAsync(saved, ct);
        await administration.RecordAsync(new AuditEntry(actor, AuditActions.VerificationRecorded, VerificationType, verificationId, null, null,
            new { saved.HangMuc, saved.DonViCungCap, saved.KetQua, saved.GhiChu }), ct);
        return Op.Ok(ToInfo(saved));
    }

    public async Task<OpResult<QuoteInfo>> IssueQuoteAsync(int id, QuoteForm form, int actor, CancellationToken ct)
    {
        var row = await repository.RequestAsync(id, ct);
        if (row is null) return Op.NotFound("Không tìm thấy yêu cầu.");
        if (row.TrangThai is not (QuoteRequestStatus.Verifying or QuoteRequestStatus.Quoted))
            return Op.Conflict($"Yêu cầu đang ở trạng thái \"{QuoteRequestStatus.Label(row.TrangThai)}\" nên không phát hành báo giá được.");
        if ((await repository.VerificationsAsync(id, ct)).Count == 0) return Op.Unprocessable("Cần ghi nhận ít nhất một kết quả xác minh trước khi báo giá.");
        var errors = QuoteCalculator.Validate(form, Now);
        if (errors.Count > 0) return Op.Unprocessable(string.Join(" ", errors.SelectMany(error => error.Value)));
        var check = await itinerary.CheckVersionAsync(row.MaPb, ct);
        if (check.Warnings.Any(warning => warning.Code is ScheduleWarningCodes.Overlap or ScheduleWarningCodes.Inactive))
            return Op.Unprocessable("Lịch trình còn xung đột giờ hoặc suất đã ngừng; hãy đề xuất thay thế trước khi báo giá.");

        await using var transaction = await repository.BeginAsync(ct);
        await repository.SupersedeAsync(id, ct);
        var total = QuoteCalculator.Total(form.Items);
        var quote = new Baogia { MaYc = id, MaPb = row.MaPb, TongGia = total, KhoanCanThu = form.Deposit, DieuKienHuy = form.CancelPolicy.Trim(),
            HanHieuLuc = form.ValidUntil, TrangThai = QuoteStatus.Sent };
        var items = form.Items.Select(item => new Hangmucbaogia { MoTa = item.Description.Trim(), DonViTinh = item.Unit.Trim(), SoLuong = item.Quantity,
            DonGia = item.UnitPrice, TrongGoi = item.IncludedInPackage }).ToList();
        if (await repository.AddQuoteAsync(quote, items, ct) is not { } quoteId)
            return Op.Conflict("Một nhân viên khác vừa phát hành báo giá cho yêu cầu này; vui lòng tải lại.");
        if (row.TrangThai == QuoteRequestStatus.Verifying && !await repository.MoveRequestAsync(id, QuoteRequestStatus.Verifying, QuoteRequestStatus.Quoted, ct))
            return Op.Conflict("Yêu cầu vừa được cập nhật bởi người khác; vui lòng tải lại.");
        await administration.RecordAsync(new AuditEntry(actor, AuditActions.QuoteIssued, QuoteType, quoteId, null, null,
            new { Total = total, form.Deposit, form.ValidUntil, Items = form.Items.Count }), ct);
        if (await repository.OwnerAsync(id, ct) is { } owner)
            await administration.NotifyAsync(owner, $"Yêu cầu tour riêng #{id} đã có báo giá", $"Báo giá #{quoteId} có hiệu lực đến {form.ValidUntil:dd/MM/yyyy HH:mm}.", ct);
        await transaction.CommitAsync(ct);
        return Op.Ok(QuoteMapper.ToInfo((await repository.QuoteAsync(quoteId, ct))!));
    }

    public async Task<OpResult<QuoteRequestInfo>> CancelAsync(int id, string? reason, int actor, CancellationToken ct)
    {
        var row = await repository.RequestAsync(id, ct);
        if (row is null) return Op.NotFound("Không tìm thấy yêu cầu.");
        if (row.TrangThai is QuoteRequestStatus.Accepted or QuoteRequestStatus.Cancelled)
            return Op.Conflict($"Yêu cầu đang ở trạng thái \"{QuoteRequestStatus.Label(row.TrangThai)}\" nên không hủy được.");
        if (!await repository.MoveRequestAsync(id, row.TrangThai, QuoteRequestStatus.Cancelled, ct))
            return Op.Conflict("Yêu cầu vừa được cập nhật bởi người khác; vui lòng tải lại.");
        await administration.RecordAsync(new AuditEntry(actor, AuditActions.PrivateRequestCancelled, RequestType, id, reason,
            new { Status = row.TrangThai }, new { Status = QuoteRequestStatus.Cancelled }), ct);
        return Op.Ok(await InfoAsync((await repository.RequestAsync(id, ct))!, ct));
    }

    /// <summary>Tác vụ nền: báo giá Sent quá hạn chuyển Expired; yêu cầu vẫn Quoted. Mỗi báo giá một giao dịch.</summary>
    public async Task<int> ExpireQuotesAsync(DateTime now, CancellationToken ct)
    {
        var expired = 0;
        foreach (var quote in await repository.ExpiredQuotesAsync(now, ct))
        {
            try
            {
                await using var transaction = await repository.BeginAsync(ct);
                if (!await repository.MoveQuoteAsync(quote.MaBg, QuoteStatus.Sent, QuoteStatus.Expired, ct)) continue;
                await administration.RecordAsync(new AuditEntry(null, AuditActions.QuoteExpired, QuoteType, quote.MaBg, null,
                    new { Status = QuoteStatus.Sent }, new { Status = QuoteStatus.Expired }), ct);
                if (await repository.OwnerAsync(quote.MaYc, ct) is { } owner)
                    await administration.NotifyAsync(owner, $"Báo giá #{quote.MaBg} đã hết hạn", "Bạn có thể đề nghị nhân viên phát hành báo giá mới.", ct);
                await transaction.CommitAsync(ct);
                expired++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Không xử lý được báo giá hết hạn {QuoteId}.", quote.MaBg);
            }
        }
        return expired;
    }

    private async Task<QuoteRequestInfo> InfoAsync(Yeucautourrieng row, CancellationToken ct) =>
        QuoteMapper.ToInfo(row, QuoteRequestService.CurrentQuote(await repository.QuotesAsync(row.MaYc, ct))?.MaBg);

    private static VerificationInfo ToInfo(Xacminhdichvu row) => new(row.MaXm, row.HangMuc, row.DonViCungCap, row.ThoiDiem, row.KetQua, row.GhiChu);
}
