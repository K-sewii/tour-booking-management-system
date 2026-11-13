using System.Text.Json;
using DongNamTrip.Contracts;
using DongNamTrip.Modules.Administration;
using DongNamTrip.Modules.Catalog;
using DongNamTrip.Modules.Persistence.Entities;

namespace DongNamTrip.Modules.Booking;

/// <summary>Cổng công khai của Booking cho module khác (Quotation tạo đơn khi khách chấp nhận báo giá).</summary>
public interface IBookingApi
{
    /// <summary>Tạo đơn tour riêng ở trạng thái Pending. Không tự mở giao dịch: chạy trong giao dịch của người gọi.</summary>
    Task<OpResult<int>> CreateFromQuoteAsync(QuoteAcceptance acceptance, int actorId, CancellationToken ct);
    Task<int?> OrderIdByQuoteAsync(int quoteId, CancellationToken ct);
}

/// <summary>Đặt tour, xem và hủy đơn của khách (UC11, UC12, UC13).</summary>
public sealed class BookingService(BookingRepository repository, ICatalogApi catalog, IAdministrationApi administration, IConfiguration configuration, TimeProvider time) : IBookingApi
{
    private DateTime Now => time.GetLocalNow().DateTime;
    private DateOnly Today => DateOnly.FromDateTime(Now);

    public async Task<OpResult<Fresh<BookingInfo>>> PlaceAsync(int accountId, BookingForm form, CancellationToken ct)
    {
        var contact = (form.Contact ?? "").Trim();
        if (form.Guests is < Limits.GuestsMin or > Limits.GuestsMax) return Op.Invalid($"Số khách từ {Limits.GuestsMin} đến {Limits.GuestsMax}.");
        if (contact.Length is 0 or > 250) return Op.Invalid("Cần nhập thông tin liên hệ (tối đa 250 ký tự).");

        await using var transaction = await repository.BeginAsync(ct);
        var open = await repository.OpenOrderAsync(accountId, form.DepartureId, ct);
        if (open is not null) return await ExistingAsync(open, form.Guests, ct);

        var hold = await catalog.HoldSeatsAsync(form.DepartureId, form.Guests, ct);
        switch (hold.Status)
        {
            case SeatHold.NotFound: return Op.NotFound("Không tìm thấy lịch khởi hành.");
            case SeatHold.Full: return Op.Conflict($"Lịch khởi hành không đủ chỗ (còn {hold.Remaining} chỗ).");
            case SeatHold.Closed: return Op.Conflict("Lịch khởi hành đã đóng hoặc không nhận đặt chỗ.");
        }
        var departure = await catalog.DepartureAsync(form.DepartureId, ct);
        if (departure is null) return Op.NotFound("Không tìm thấy lịch khởi hành.");

        var now = Now;
        var price = departure.Price * form.Guests;
        var holdHours = configuration.GetValue("Booking:HoldHours", 48.0);
        var holdUntil = now.AddHours(holdHours);
        if (holdUntil > departure.StartsAt) holdUntil = departure.StartsAt;
        var order = new Dondat
        {
            MaTk = accountId,
            MaLkh = form.DepartureId,
            SoKhach = form.Guests,
            GiaChot = price,
            DieuKienHuy = string.IsNullOrWhiteSpace(departure.CancelPolicy) ? CancelPolicyDefaults.Standard : departure.CancelPolicy,
            HanGiuCho = holdUntil,
            TrangThai = OrderStatus.Pending,
            NgayDat = DateTime.UtcNow,
            NgayDi = DateOnly.FromDateTime(departure.StartsAt),
            KhoanCanThu = OrderRules.Deposit(price, (decimal)configuration.GetValue("Booking:DepositRate", 0.3)),
            LienHe = contact
        };
        if (await repository.AddOrderAsync(order, ct) is null)
        {
            // Gửi lặp đồng thời: bỏ giao dịch (trả lại chỗ vừa giữ) rồi trả đơn đã có.
            await transaction.RollbackAsync(ct);
            var existing = await repository.OpenOrderAsync(accountId, form.DepartureId, ct);
            return existing is null ? Op.Conflict("Không thể tạo đơn, vui lòng thử lại.") : await ExistingAsync(existing, form.Guests, ct);
        }
        await transaction.CommitAsync(ct);
        return Op.Ok(new Fresh<BookingInfo>((await InfosAsync([order], ct))[0], true));
    }

    private async Task<OpResult<Fresh<BookingInfo>>> ExistingAsync(Dondat open, int guests, CancellationToken ct) =>
        open.SoKhach == guests
            ? Op.Ok(new Fresh<BookingInfo>((await InfosAsync([open], ct))[0], false))
            : Op.Conflict($"Bạn đã có đơn chờ xử lý cho lịch này với {open.SoKhach} khách; hãy hủy đơn đó trước khi đổi số khách.");

    public async Task<List<BookingInfo>> ListAsync(int accountId, CancellationToken ct) =>
        await InfosAsync(await repository.OrdersAsync(accountId, ct), ct);

    public async Task<BookingDetail?> GetAsync(int accountId, int id, CancellationToken ct)
    {
        var order = await repository.OrderAsync(accountId, id, ct);
        return order is null ? null : await DetailAsync(order, ct);
    }

    public async Task<OpResult<BookingInfo>> RequestCancelAsync(int accountId, int id, string? reason, CancellationToken ct)
    {
        var text = (reason ?? "").Trim();
        if (text.Length is 0 or > 1000) return Op.Invalid("Cần nhập lý do hủy (tối đa 1000 ký tự).");
        var order = await repository.OrderAsync(accountId, id, ct);
        if (order is null) return Op.NotFound("Không tìm thấy đơn đặt.");
        if (order.TrangThai is not (OrderStatus.Pending or OrderStatus.Confirmed))
            return Op.Conflict($"Đơn đang ở trạng thái \"{OrderStatus.Label(order.TrangThai)}\" nên không gửi yêu cầu hủy được.");
        if (order.NgayDi is { } tripDate && tripDate < Today)
            return Op.Conflict("Ngày đi đã qua; đơn chờ nhân viên hoàn thành, không hủy được.");

        await using var transaction = await repository.BeginAsync(ct);
        if (!await repository.MoveAsync(id, order.TrangThai, OrderStatus.CancelRequested, ct))
            return Op.Conflict("Đơn vừa được cập nhật bởi người khác; vui lòng tải lại.");
        await repository.SetCancelAsync(id, text, order.TrangThai, ct);
        await transaction.CommitAsync(ct);
        return Op.Ok((await InfosAsync([(await repository.OrderAsync(accountId, id, ct))!], ct))[0]);
    }

    public async Task<OpResult<int>> CreateFromQuoteAsync(QuoteAcceptance acceptance, int actorId, CancellationToken ct)
    {
        var order = new Dondat
        {
            MaTk = acceptance.AccountId,
            MaBg = acceptance.QuoteId,
            SoKhach = acceptance.Guests,
            GiaChot = acceptance.Amount,
            KhoanCanThu = acceptance.Deposit,
            DieuKienHuy = acceptance.CancelPolicy,
            NgayDi = acceptance.TripStart,
            LienHe = acceptance.Contact,
            HanGiuCho = null,
            TrangThai = OrderStatus.Pending,
            NgayDat = DateTime.UtcNow
        };
        if (await repository.AddOrderAsync(order, ct) is { } id) return Op.Ok(id);
        var existing = await repository.ByQuoteAsync(acceptance.QuoteId, ct);
        return existing is null ? Op.Conflict("Không thể tạo đơn từ báo giá.") : Op.Ok(existing.MaDon);
    }

    public async Task<int?> OrderIdByQuoteAsync(int quoteId, CancellationToken ct) => (await repository.ByQuoteAsync(quoteId, ct))?.MaDon;

    // ---- Ánh xạ dùng chung với BookingAdminService ----

    internal async Task<List<BookingInfo>> InfosAsync(IReadOnlyList<Dondat> orders, CancellationToken ct)
    {
        var titles = await TitlesAsync(orders, ct);
        return orders.Select(order => ToInfo(order, titles)).ToList();
    }

    internal async Task<Dictionary<int, string>> TitlesAsync(IReadOnlyList<Dondat> orders, CancellationToken ct)
    {
        var departures = await catalog.DeparturesAsync(orders.Where(order => order.MaLkh is not null).Select(order => order.MaLkh!.Value).Distinct().ToArray(), ct);
        return orders.ToDictionary(order => order.MaDon, order => order.MaLkh is { } id && departures.TryGetValue(id, out var info)
            ? info.TourName : $"Tour riêng theo báo giá #{order.MaBg}");
    }

    internal static BookingInfo ToInfo(Dondat order, IReadOnlyDictionary<int, string> titles) => new(order.MaDon,
        order.MaLkh is null ? BookingSources.Quote : BookingSources.Departure, order.MaLkh, order.MaBg, titles.GetValueOrDefault(order.MaDon, ""),
        order.NgayDat, order.SoKhach, order.GiaChot, order.KhoanCanThu ?? 0, order.HanGiuCho, order.NgayDi, order.TrangThai);

    internal async Task<BookingDetail> DetailAsync(Dondat order, CancellationToken ct)
    {
        var info = (await InfosAsync([order], ct))[0];
        var money = order.Giaodichtien.OrderByDescending(entry => entry.ThoiDiem)
            .Select(entry => new MoneyEntry(entry.MaGd, entry.LoaiGd, entry.SoTien, entry.ThoiDiem, entry.MaThamChieu)).ToList();
        var showReason = order.TrangThai is OrderStatus.CancelRequested or OrderStatus.Cancelled;
        return new BookingDetail(info, order.LienHe ?? "", order.DieuKienHuy, showReason ? order.LyDoHuy : null,
            OrderRules.Paid(money), OrderRules.Refunded(money), OrderRules.Outstanding(order.GiaChot, money), OrderRules.Refundable(money), money,
            await ProgramAsync(order, ct), await AdjustmentsAsync(order.MaDon, ct));
    }

    /// <summary>Chương trình của tour trong đơn tour có sẵn; đơn tour riêng trả rỗng vì lịch trình nằm ở phiên bản gắn với báo giá (module Quotation).</summary>
    // shortcut: đọc chương trình hiện tại của tour (TourAdmin chặn sửa điểm dừng khi còn chỗ giữ), chưa chụp lại lúc đặt; nâng cấp khi cần bảo vệ đơn đã hủy hoặc hoàn thành.
    private async Task<List<ProgramStop>> ProgramAsync(Dondat order, CancellationToken ct) =>
        order.MaLkh is { } departureId && await catalog.DepartureAsync(departureId, ct) is { } departure ? await catalog.ProgramAsync(departure.TourId, ct) : [];

    /// <summary>Lịch sử điều chỉnh lấy từ nhật ký của đơn (không thêm bảng); mới nhất trước. Ý kiến đồng ý của khách nằm cuối lý do và không hiển thị lại.</summary>
    private async Task<List<BookingAdjustment>> AdjustmentsAsync(int orderId, CancellationToken ct)
    {
        var result = new List<BookingAdjustment>();
        foreach (var row in await administration.HistoryAsync("DONDAT", orderId, ct))
        {
            if (row.Action != OrderRules.AdjustAction) continue;
            var change = "";
            decimal? price = null;
            try
            {
                using var after = JsonDocument.Parse(row.After ?? "{}");
                if (after.RootElement.TryGetProperty("Change", out var text)) change = text.GetString() ?? "";
                if (after.RootElement.TryGetProperty("Price", out var value) && value.TryGetDecimal(out var amount)) price = amount;
            }
            catch (JsonException) { }
            var reason = (row.Reason ?? "").Split(OrderRules.ConsentSeparator)[0];
            result.Add(new BookingAdjustment(row.At, change, reason, price));
        }
        return result;
    }
}
