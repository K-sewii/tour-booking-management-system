using System.Text.Json;
using DongNamTrip.Contracts;
using DongNamTrip.Modules.Itinerary;
using DongNamTrip.Modules.Persistence.Entities;

namespace DongNamTrip.Modules.Quotation;

public sealed class QuotationService(QuotationRepository repository, IItineraryApi itineraries)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<List<PrivateTourRequest>> ListAsync(int accountId, bool staff, CancellationToken ct)
    {
        var ids = await repository.RequestIdsAsync(accountId, staff, ct);
        var result = new List<PrivateTourRequest>(ids.Count);
        foreach (var id in ids)
            if (await GetAsync(id, accountId, staff, ct) is { } request) result.Add(request);
        return result;
    }

    public async Task<PrivateTourRequest?> GetAsync(int id, int accountId, bool staff, CancellationToken ct) =>
        await repository.RequestAsync(id, accountId, staff, ct) is { } row ? ToRequest(row) : null;

    public async Task<OpResult<PrivateTourRequest>> SubmitAsync(int itineraryId, int accountId, PrivateTourRequestDraft draft, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(draft.Contact)) return Op.Invalid("Cần thông tin liên hệ để gửi yêu cầu.");
        var trip = await itineraries.GetAsync(itineraryId, accountId, ct);
        if (trip is null) return Op.NotFound("Không tìm thấy lịch trình của bạn.");
        if (trip.StartDate < DateOnly.FromDateTime(DateTime.Today) || trip.Guests < 1 || trip.Stops.Length == 0)
            return Op.Invalid("Cần ngày đi, số khách và địa điểm hợp lệ trước khi gửi.");
        var id = await repository.SubmitAsync(itineraryId, JsonSerializer.Serialize(trip, Json), trip.Guests, draft, ct);
        return Op.Ok((await GetAsync(id, accountId, false, ct))!);
    }

    public async Task<OpResult<ServiceVerification>> VerifyAsync(int id, ServiceVerificationDraft draft, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(draft.Item) || string.IsNullOrWhiteSpace(draft.Supplier)
            || draft.Result is not ("Available" or "Unavailable" or "NeedsInfo"))
            return Op.Invalid("Cần hạng mục, đơn vị cung cấp và kết quả xác minh hợp lệ.");
        var result = await repository.VerifyAsync(id, draft, ct);
        return result.Status switch
        {
            QuotationWriteStatus.NotFound => Op.NotFound("Không tìm thấy yêu cầu."),
            QuotationWriteStatus.Conflict => Op.Conflict("Yêu cầu đã được chốt."),
            _ => Op.Ok(ToVerification(result.Value!))
        };
    }

    public async Task<OpResult<PrivateTourRequest>> IssueAsync(int id, QuoteDraft draft, CancellationToken ct)
    {
        if (draft.Lines is not { Length: > 0 } || draft.ExpiresAt <= DateTime.UtcNow
            || string.IsNullOrWhiteSpace(draft.CancellationTerms)
            || draft.Lines.Any(line => string.IsNullOrWhiteSpace(line.Description) || string.IsNullOrWhiteSpace(line.Unit)
                || line.Quantity < 1 || line.UnitPrice < 0))
            return Op.Invalid("Báo giá cần hạng mục, điều kiện hủy và hạn hiệu lực tương lai.");
        decimal total;
        try { total = draft.Lines.Sum(line => checked(line.Quantity * line.UnitPrice)); }
        catch (OverflowException) { return Op.Invalid("Tổng giá vượt giới hạn."); }
        if (total > 999999999999999999 || draft.AmountDue < 0 || draft.AmountDue > total)
            return Op.Invalid("Khoản cần thu phải nằm trong tổng giá.");
        var result = await repository.IssueAsync(id, draft, total, ct);
        return result.Status switch
        {
            QuotationWriteStatus.NotFound => Op.NotFound("Không tìm thấy yêu cầu."),
            QuotationWriteStatus.Conflict => Op.Conflict("Yêu cầu đã được chốt."),
            QuotationWriteStatus.NeedsVerification => Op.Conflict("Cần xác minh ít nhất một dịch vụ có thể tổ chức trước khi báo giá."),
            _ => Op.Ok((await GetAsync(id, 0, true, ct))!)
        };
    }

    public async Task<OpResult<PrivateTourOrder>> AcceptAsync(int requestId, int quoteId, int accountId, CancellationToken ct)
    {
        var quote = await repository.QuoteAsync(requestId, quoteId, accountId, ct);
        if (quote is null) return Op.NotFound("Không tìm thấy báo giá của bạn.");
        if (quote.Dondat is null)
        {
            if (quote.TrangThai != "Issued" || quote.HanHieuLuc <= DateTime.UtcNow)
                return Op.Conflict("Báo giá đã hết hạn hoặc đã được thay thế.");
            var version = quote.MaYcNavigation.MaPbNavigation;
            var current = await itineraries.GetAsync(version.MaLt, accountId, ct);
            var snapshot = JsonSerializer.Deserialize<Contracts.Itinerary>(version.NoiDungJson, Json);
            if (current is null || snapshot is null || !SamePlan(current, snapshot))
                return Op.Conflict("Lịch trình đã đổi sau khi gửi yêu cầu; vui lòng xin báo giá mới.");
        }
        var result = await repository.AcceptAsync(requestId, quoteId, accountId, ct);
        return result.Status switch
        {
            QuotationWriteStatus.NotFound => Op.NotFound("Không tìm thấy báo giá của bạn."),
            QuotationWriteStatus.Conflict => Op.Conflict("Báo giá đã hết hạn hoặc đã được thay thế."),
            QuotationWriteStatus.StaleItinerary => Op.Conflict("Lịch trình đã đổi sau khi gửi yêu cầu; vui lòng xin báo giá mới."),
            _ => Op.Ok(ToOrder(result.Value!))
        };
    }

    public async Task<List<PrivateTourOrder>> OrdersAsync(int accountId, CancellationToken ct) =>
        (await repository.OrdersAsync(accountId, ct)).Select(ToOrder).ToList();

    private static bool SamePlan(Contracts.Itinerary left, Contracts.Itinerary right) =>
        JsonSerializer.Serialize(PlanShape(left), Json) == JsonSerializer.Serialize(PlanShape(right), Json);

    private static object PlanShape(Contracts.Itinerary item) => new
    {
        item.Id, item.UserId, item.Name, item.StartDate, item.Guests, item.DeparturePoint, item.Transport, item.Budget,
        Stops = item.Stops.Select(stop => new { stop.DestinationId, stop.Day, stop.Order, stop.Note, stop.SlotIds }).ToArray()
    };

    private static PrivateTourRequest ToRequest(Yeucautourrieng row)
    {
        var version = row.MaPbNavigation;
        var snapshot = JsonSerializer.Deserialize<Contracts.Itinerary>(version.NoiDungJson, Json)!;
        return new(row.MaYc, version.MaLt, version.SoPhienBan, version.MaLtNavigation.MaTk,
            version.MaLtNavigation.MaTkNavigation.HoTen, row.LienHe, row.NhuCauDichVu, row.NgayGui, row.TrangThai,
            snapshot, row.Xacminhdichvu.OrderBy(item => item.MaXm).Select(ToVerification).ToList(),
            row.Baogia.OrderBy(item => item.MaBg).Select(item => new PrivateTourQuote(item.MaBg, row.MaYc,
                version.SoPhienBan, item.TongGia, item.KhoanCanThu, item.DieuKienHuy, item.HanHieuLuc,
                item.TrangThai == "Issued" && item.HanHieuLuc <= DateTime.UtcNow ? "Expired" : item.TrangThai,
                item.Hangmucbaogia.OrderBy(line => line.MaHm).Select(line => new QuoteLine(line.MaHm, line.MoTa,
                    line.DonViTinh, line.SoLuong, line.DonGia)).ToList(), item.Dondat?.MaDon)).ToList());
    }

    private static ServiceVerification ToVerification(Xacminhdichvu item) =>
        new(item.MaXm, item.HangMuc, item.DonViCungCap, item.KetQua, item.GhiChu, item.ThoiDiem);
    private static PrivateTourOrder ToOrder(Dondat row) =>
        new(row.MaDon, row.MaBg!.Value, row.MaTk, row.SoKhach, row.GiaChot, row.DieuKienHuy, row.TrangThai, row.NgayDat);
}
