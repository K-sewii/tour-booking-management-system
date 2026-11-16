using DongNamTrip.Contracts;
using DongNamTrip.Modules.Administration;
using DongNamTrip.Modules.Booking;
using DongNamTrip.Modules.Itinerary;
using DongNamTrip.Modules.Persistence.Entities;

namespace DongNamTrip.Modules.Quotation;

/// <summary>Khách gửi yêu cầu tour riêng, xem báo giá, chấp nhận hoặc đề nghị đổi (UC09, UC10).</summary>
public sealed class QuoteRequestService(QuotationRepository repository, IItineraryApi itinerary, IBookingApi booking, IAdministrationApi administration, TimeProvider time)
{
    private const string RequestType = "YEUCAUTOURRIENG";
    private const string QuoteType = "BAOGIA";

    private DateTime Now => time.GetLocalNow().DateTime;

    public async Task<OpResult<Fresh<QuoteRequestInfo>>> SubmitAsync(int accountId, QuoteRequestForm form, CancellationToken ct)
    {
        var contact = (form.Contact ?? "").Trim();
        if (form.Guests is < Limits.GuestsMin or > Limits.GuestsMax) return Op.Invalid($"Số khách từ {Limits.GuestsMin} đến {Limits.GuestsMax}.");
        if (contact.Length is 0 or > 250) return Op.Invalid("Cần nhập thông tin liên hệ (tối đa 250 ký tự).");
        var needs = string.IsNullOrWhiteSpace(form.Needs) ? null : form.Needs.Trim();
        if (needs?.Length > 2000) return Op.Invalid("Nhu cầu dịch vụ tối đa 2000 ký tự.");
        var trip = await itinerary.GetAsync(form.ItineraryId, accountId, ct);
        if (trip is null) return Op.NotFound("Không tìm thấy lịch trình của bạn.");
        if (trip.StartDate < DateOnly.FromDateTime(Now)) return Op.Invalid("Ngày đi của lịch trình đã qua; hãy cập nhật ngày đi trước khi gửi yêu cầu.");

        await using var transaction = await repository.BeginAsync(ct);
        var version = await itinerary.CreateVersionAsync(form.ItineraryId, accountId, ct);
        if (!version.IsOk) return new OpResult<Fresh<QuoteRequestInfo>>(null, version.Status, version.Message);
        var existing = await repository.OpenRequestAsync(accountId, form.ItineraryId, ct);
        if (existing is not null)
        {
            await transaction.RollbackAsync(ct); // bỏ phiên bản vừa tạo
            return Op.Ok(new Fresh<QuoteRequestInfo>(await InfoAsync(existing, ct), false));
        }
        var id = await repository.AddRequestAsync(new Yeucautourrieng
        {
            MaPb = version.Value!.VersionId, SoKhach = form.Guests, LienHe = contact, NhuCauDichVu = needs,
            NgayGui = DateTime.UtcNow, TrangThai = QuoteRequestStatus.Submitted
        }, ct);
        await transaction.CommitAsync(ct);
        return Op.Ok(new Fresh<QuoteRequestInfo>(await InfoAsync((await repository.RequestAsync(id, ct))!, ct), true));
    }

    public async Task<List<QuoteRequestInfo>> ListMineAsync(int accountId, CancellationToken ct)
    {
        var rows = await repository.MineAsync(accountId, ct);
        var current = await repository.CurrentQuoteIdsAsync(rows.Select(row => row.MaYc).ToArray(), ct);
        return rows.Select(row => QuoteMapper.ToInfo(row, current.TryGetValue(row.MaYc, out var quote) ? quote : null)).ToList();
    }

    public async Task<QuoteRequestDetail?> GetMineAsync(int accountId, int id, CancellationToken ct)
    {
        if (await repository.OwnerAsync(id, ct) != accountId) return null;
        var row = (await repository.RequestAsync(id, ct))!;
        var version = (await itinerary.VersionAsync(row.MaPb, ct))!;
        var quotes = await repository.QuotesAsync(id, ct);
        var current = CurrentQuote(quotes);
        int? orderId = current is { TrangThai: QuoteStatus.Accepted } ? await booking.OrderIdByQuoteAsync(current.MaBg, ct) : null;
        return new QuoteRequestDetail(await InfoAsync(row, ct), version, current is null ? null : QuoteMapper.ToInfo(current), orderId);
    }

    /// <summary>Lịch trình chốt của một báo giá (phiên bản gắn báo giá, QT05); null nếu không phải của khách.</summary>
    public async Task<List<ProgramStop>?> ProgramAsync(int accountId, int quoteId, CancellationToken ct)
    {
        var quote = await repository.QuoteAsync(quoteId, ct);
        return quote is null || await repository.OwnerAsync(quote.MaYc, ct) != accountId ? null : await itinerary.ProgramAsync(quote.MaPb, ct);
    }

    public async Task<OpResult<QuoteRequestInfo>> CancelAsync(int accountId, int id, CancellationToken ct)
    {
        if (await repository.OwnerAsync(id, ct) != accountId) return Op.NotFound("Không tìm thấy yêu cầu.");
        var row = (await repository.RequestAsync(id, ct))!;
        if (row.TrangThai is QuoteRequestStatus.Accepted or QuoteRequestStatus.Cancelled)
            return Op.Conflict($"Yêu cầu đang ở trạng thái \"{QuoteRequestStatus.Label(row.TrangThai)}\" nên không hủy được.");
        // Báo giá Sent (nếu có) giữ nguyên và tự hết hạn; không chấp nhận được vì yêu cầu không còn Quoted.
        if (!await repository.MoveRequestAsync(id, row.TrangThai, QuoteRequestStatus.Cancelled, ct))
            return Op.Conflict("Yêu cầu vừa được cập nhật bởi người khác; vui lòng tải lại.");
        return Op.Ok(await InfoAsync((await repository.RequestAsync(id, ct))!, ct));
    }

    public async Task<OpResult<Fresh<QuoteAccepted>>> AcceptQuoteAsync(int accountId, int quoteId, CancellationToken ct)
    {
        await using var transaction = await repository.BeginAsync(ct);
        var quote = await repository.QuoteAsync(quoteId, ct);
        if (quote is null || await repository.OwnerAsync(quote.MaYc, ct) != accountId) return Op.NotFound("Không tìm thấy báo giá.");
        if (quote.TrangThai == QuoteStatus.Accepted) return await AlreadyAcceptedAsync(quote, ct);
        if (quote.TrangThai != QuoteStatus.Sent) return Op.Conflict($"Báo giá đang ở trạng thái \"{QuoteStatus.Label(quote.TrangThai)}\" nên không chấp nhận được.");
        if (quote.HanHieuLuc < Now) return Op.Conflict("Báo giá đã hết hạn.");
        var request = (await repository.RequestAsync(quote.MaYc, ct))!;
        if (request.TrangThai != QuoteRequestStatus.Quoted) return Op.Conflict("Yêu cầu không còn ở trạng thái đã báo giá.");
        var version = await itinerary.VersionAsync(request.MaPb, ct);
        if (version is null) return Op.Conflict("Không tìm thấy phiên bản lịch trình của yêu cầu.");
        // Đơn tour riêng không có hạn giữ chỗ và khách không hủy được khi NgayDi đã qua, nên không tạo đơn cho ngày đã qua.
        if (version.StartDate < DateOnly.FromDateTime(Now)) return Op.Conflict("Ngày đi của lịch trình đã qua nên không chấp nhận báo giá được; hãy đề nghị đổi báo giá.");

        if (!await repository.MoveQuoteAsync(quoteId, QuoteStatus.Sent, QuoteStatus.Accepted, ct))
        {
            var current = await repository.QuoteAsync(quoteId, ct);
            return current is { TrangThai: QuoteStatus.Accepted } ? await AlreadyAcceptedAsync(current, ct) : Op.Conflict("Báo giá vừa được cập nhật bởi người khác.");
        }
        if (!await repository.MoveRequestAsync(request.MaYc, QuoteRequestStatus.Quoted, QuoteRequestStatus.Accepted, ct))
            return Op.Conflict("Yêu cầu vừa được cập nhật bởi người khác.");
        var order = await booking.CreateFromQuoteAsync(new QuoteAcceptance(quoteId, request.MaYc, accountId, request.SoKhach, quote.TongGia, quote.KhoanCanThu,
            quote.DieuKienHuy, version.StartDate, request.LienHe), accountId, ct);
        if (!order.IsOk) return new OpResult<Fresh<QuoteAccepted>>(null, order.Status, order.Message);
        await administration.RecordAsync(new AuditEntry(accountId, AuditActions.QuoteAccepted, QuoteType, quoteId, null,
            new { Status = QuoteStatus.Sent }, new { Status = QuoteStatus.Accepted, OrderId = order.Value }), ct);
        await transaction.CommitAsync(ct);
        return Op.Ok(new Fresh<QuoteAccepted>(new QuoteAccepted(quoteId, order.Value, quote.TongGia, quote.KhoanCanThu), true));
    }

    private async Task<OpResult<Fresh<QuoteAccepted>>> AlreadyAcceptedAsync(Baogia quote, CancellationToken ct)
    {
        var orderId = await booking.OrderIdByQuoteAsync(quote.MaBg, ct);
        return orderId is null ? Op.Conflict("Báo giá đã chấp nhận nhưng chưa có đơn.")
            : Op.Ok(new Fresh<QuoteAccepted>(new QuoteAccepted(quote.MaBg, orderId.Value, quote.TongGia, quote.KhoanCanThu), false));
    }

    public async Task<OpResult<QuoteRequestInfo>> AskChangeAsync(int accountId, int quoteId, string? note, CancellationToken ct)
    {
        var text = (note ?? "").Trim();
        if (text.Length is 0 or > 1000) return Op.Invalid("Cần nhập nội dung đề nghị đổi (tối đa 1000 ký tự).");
        await using var transaction = await repository.BeginAsync(ct);
        var quote = await repository.QuoteAsync(quoteId, ct);
        if (quote is null || await repository.OwnerAsync(quote.MaYc, ct) != accountId) return Op.NotFound("Không tìm thấy báo giá.");
        if (quote.TrangThai is QuoteStatus.Accepted or QuoteStatus.Superseded)
            return Op.Conflict($"Báo giá đang ở trạng thái \"{QuoteStatus.Label(quote.TrangThai)}\" nên không đề nghị đổi được.");
        if (!await repository.MoveRequestAsync(quote.MaYc, QuoteRequestStatus.Quoted, QuoteRequestStatus.Verifying, ct))
            return Op.Conflict("Yêu cầu không còn ở trạng thái đã báo giá.");
        await repository.SupersedeAsync(quote.MaYc, ct); // chỉ báo giá còn Sent; báo giá đã hết hạn giữ nguyên
        await administration.RecordAsync(new AuditEntry(accountId, AuditActions.QuoteChangeRequested, RequestType, quote.MaYc, text,
            new { Status = QuoteRequestStatus.Quoted }, new { Status = QuoteRequestStatus.Verifying }), ct);
        await transaction.CommitAsync(ct);
        return Op.Ok(await InfoAsync((await repository.RequestAsync(quote.MaYc, ct))!, ct));
    }

    // ---- Ánh xạ ----

    internal static Baogia? CurrentQuote(IReadOnlyList<Baogia> quotes) => quotes.Where(quote => quote.TrangThai != QuoteStatus.Superseded).MaxBy(quote => quote.MaBg);

    internal async Task<QuoteRequestInfo> InfoAsync(Yeucautourrieng row, CancellationToken ct) =>
        QuoteMapper.ToInfo(row, CurrentQuote(await repository.QuotesAsync(row.MaYc, ct))?.MaBg);
}

internal static class QuoteMapper
{
    public static QuoteRequestInfo ToInfo(Yeucautourrieng row, int? currentQuoteId) => new(row.MaYc, row.MaPbNavigation.MaLt, row.MaPb, row.MaPbNavigation.SoPhienBan,
        row.NgayGui, row.SoKhach, row.TrangThai, currentQuoteId);

    public static QuoteInfo ToInfo(Baogia quote) => new(quote.MaBg, quote.MaYc, quote.MaPb, quote.TongGia, quote.KhoanCanThu, quote.DieuKienHuy, quote.HanHieuLuc, quote.TrangThai,
        quote.Hangmucbaogia.OrderBy(item => item.MaHm).Select(item => new QuoteItemForm(item.MoTa, item.DonViTinh, item.SoLuong, item.DonGia, item.TrongGoi)).ToList());
}
