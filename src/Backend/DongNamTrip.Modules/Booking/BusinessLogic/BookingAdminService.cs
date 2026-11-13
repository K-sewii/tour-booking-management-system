using DongNamTrip.Contracts;
using DongNamTrip.Modules.Administration;
using DongNamTrip.Modules.Catalog;
using DongNamTrip.Modules.Narration;
using DongNamTrip.Modules.Persistence.Entities;

namespace DongNamTrip.Modules.Booking;

/// <summary>Xử lý đơn của nhân viên: xác nhận, điều chỉnh, duyệt hủy, hoàn thành, ghi thu hoàn và hết hạn giữ chỗ (UC13, UC19, UC20).</summary>
public sealed class BookingAdminService(BookingRepository repository, BookingService bookings, ICatalogApi catalog, INarrationApi narration,
    IAdministrationApi administration, IConfiguration configuration, TimeProvider time, ILogger<BookingAdminService> logger)
{
    private const string OrderType = "DONDAT";
    private const string MoneyType = "GIAODICHTIEN";

    private DateTime Now => time.GetLocalNow().DateTime;
    private DateOnly Today => DateOnly.FromDateTime(Now);

    public async Task<PagedResult<AdminBooking>> ListAsync(BookingFilter filter, CancellationToken ct)
    {
        var page = Math.Clamp(filter.Page ?? 1, 1, Limits.MaxPage);
        var size = Math.Clamp(filter.Size ?? 20, 1, Limits.MaxPageSize);
        var (rows, total) = await repository.ListAsync(filter, Today, (page - 1) * size, size, ct);
        var names = await administration.AccountNamesAsync(rows.Select(row => row.MaTk).Distinct().ToArray(), ct);
        var titles = await bookings.TitlesAsync(rows, ct);
        return new PagedResult<AdminBooking>(rows.Select(row => ToAdmin(row, names, titles)).ToList(), total, page, size);
    }

    public async Task<AdminBookingDetail?> GetAsync(int id, CancellationToken ct)
    {
        var order = await repository.GetAsync(id, ct);
        return order is null ? null : await DetailAsync(order, ct);
    }

    public async Task<OpResult<AdminBooking>> ConfirmAsync(int id, int actor, CancellationToken ct)
    {
        // Khóa đơn rồi mới đọc số đã thu: một khoản hoàn ghi xen giữa không làm đơn được xác nhận khi chưa đủ khoản cần thu.
        await using var transaction = await repository.BeginAsync(ct);
        if (!await repository.LockOrderAsync(id, ct)) return Op.NotFound("Không tìm thấy đơn đặt.");
        var order = (await repository.GetAsync(id, ct))!;
        if (order.TrangThai is not (OrderStatus.Pending or OrderStatus.Expired))
            return Op.Conflict($"Đơn đang ở trạng thái \"{OrderStatus.Label(order.TrangThai)}\" nên không xác nhận được.");
        var missing = Missing(order);
        if (missing.Count > 0) return Op.Unprocessable("Chưa đủ điều kiện xác nhận: " + string.Join("; ", missing));

        if (order.TrangThai == OrderStatus.Expired && order.MaLkh is { } departureId)
        {
            // Thu muộn: giữ lại chỗ, lịch phải còn Open và giá không đổi.
            var departure = await catalog.DepartureAsync(departureId, ct);
            if (departure is null) return Op.Conflict("Lịch khởi hành không còn tồn tại.");
            if (departure.Price * order.SoKhach != order.GiaChot)
                return Op.Conflict("Giá lịch khởi hành đã đổi so với giá chốt của đơn; hãy hoàn tiền cho khách thay vì xác nhận.");
            var hold = await catalog.HoldSeatsAsync(departureId, order.SoKhach, ct);
            if (hold.Status != SeatHold.Held)
                return Op.Conflict($"Không giữ lại được chỗ cho đơn hết hạn (còn {hold.Remaining} chỗ); hãy hoàn tiền cho khách.");
        }
        if (!await repository.MoveAsync(id, order.TrangThai, OrderStatus.Confirmed, ct))
            return Op.Conflict("Đơn vừa được cập nhật bởi người khác; vui lòng tải lại.");
        if (order.NgayDi is { } tripDate)
        {
            var window = new ListenRightGrant(id, order.MaTk, tripDate.AddDays(-1).ToDateTime(TimeOnly.MinValue),
                tripDate.AddDays(configuration.GetValue("Booking:ListenDays", 7)).ToDateTime(new TimeOnly(23, 59, 59)));
            var grant = await narration.GrantListenRightAsync(window, actor, ct);
            if (!grant.IsOk) return Op.Conflict("Không cấp được quyền nghe thuyết minh: " + grant.Message);
        }
        await administration.RecordAsync(new AuditEntry(actor, "Xác nhận đơn", OrderType, id, null, new { Status = order.TrangThai }, new { Status = OrderStatus.Confirmed }), ct);
        await administration.NotifyAsync(order.MaTk, $"Đơn #{id} đã được xác nhận", "Đơn đặt tour của bạn đã được nhân viên xác nhận.", ct);
        await transaction.CommitAsync(ct);
        return Op.Ok(await AdminAsync(id, ct));
    }

    public async Task<OpResult<AdminBookingDetail>> AdjustAsync(int id, AdjustForm form, int actor, CancellationToken ct)
    {
        var change = (form.Change ?? "").Trim();
        var reason = (form.Reason ?? "").Trim();
        var consent = (form.CustomerConsent ?? "").Trim();
        if (change.Length == 0 || reason.Length == 0 || consent.Length == 0) return Op.Invalid("Cần nhập nội dung điều chỉnh, lý do và ý kiến đồng ý của khách.");
        if (form.NewPrice is <= 0) return Op.Invalid("Giá mới phải lớn hơn 0.");
        if (form.NewPrice is { } rounded && rounded != Math.Round(rounded)) return Op.Invalid("Giá mới phải là số nguyên VND.");
        var order = await repository.GetAsync(id, ct);
        if (order is null) return Op.NotFound("Không tìm thấy đơn đặt.");
        if (order.TrangThai != OrderStatus.Confirmed) return Op.Conflict("Chỉ điều chỉnh được đơn đã xác nhận.");

        await using var transaction = await repository.BeginAsync(ct);
        object after = new { Change = change, Price = order.GiaChot, Deposit = order.KhoanCanThu };
        if (form.NewPrice is { } price)
        {
            var deposit = Math.Min(order.KhoanCanThu ?? 0, price);
            if (!await repository.UpdatePriceAsync(id, price, deposit, ct)) return Op.NotFound("Không tìm thấy đơn đặt.");
            after = new { Change = change, Price = price, Deposit = deposit };
        }
        await administration.RecordAsync(new AuditEntry(actor, OrderRules.AdjustAction, OrderType, id, reason + OrderRules.ConsentSeparator + consent,
            new { Price = order.GiaChot, Deposit = order.KhoanCanThu }, after), ct);
        await administration.NotifyAsync(order.MaTk, $"Đơn #{id} đã được điều chỉnh", change, ct);
        await transaction.CommitAsync(ct);
        return Op.Ok((await GetAsync(id, ct))!);
    }

    public async Task<OpResult<AdminBooking>> DecideCancelAsync(int id, CancelDecision decision, int actor, CancellationToken ct)
    {
        var reason = (decision.Reason ?? "").Trim();
        if (reason.Length == 0) return Op.Invalid("Cần nhập lý do.");
        var order = await repository.GetAsync(id, ct);
        if (order is null) return Op.NotFound("Không tìm thấy đơn đặt.");
        if (order.TrangThai != OrderStatus.CancelRequested) return Op.Conflict("Đơn không ở trạng thái chờ hủy.");

        await using var transaction = await repository.BeginAsync(ct);
        if (decision.Approve)
        {
            if (!await repository.MoveAsync(id, OrderStatus.CancelRequested, OrderStatus.Cancelled, ct))
                return Op.Conflict("Đơn vừa được cập nhật bởi người khác; vui lòng tải lại.");
            if (order.MaLkh is { } departureId) await catalog.ReleaseSeatsAsync(departureId, order.SoKhach, ct);
            await narration.RevokeListenRightAsync(id, actor, ct);
            await administration.RecordAsync(new AuditEntry(actor, "Duyệt hủy đơn", OrderType, id, reason, new { Status = order.TrangThai }, new { Status = OrderStatus.Cancelled }), ct);
            await administration.NotifyAsync(order.MaTk, $"Đơn #{id} đã được hủy", $"Yêu cầu hủy đơn đã được duyệt. {reason}", ct);
        }
        else
        {
            var previous = order.TrangThaiTruocHuy is OrderStatus.Pending or OrderStatus.Confirmed ? order.TrangThaiTruocHuy : OrderStatus.Pending;
            switch (await repository.RestoreFromCancelAsync(id, previous, ct))
            {
                case RestoreOutcome.Lost: return Op.Conflict("Đơn vừa được cập nhật bởi người khác; vui lòng tải lại.");
                case RestoreOutcome.Duplicate:
                    return Op.Conflict("Khách đã có đơn chờ khác cho lịch này; hãy duyệt hủy đơn này hoặc xử lý đơn kia.");
            }
            await administration.RecordAsync(new AuditEntry(actor, "Từ chối hủy đơn", OrderType, id, reason, new { Status = order.TrangThai }, new { Status = previous }), ct);
            await administration.NotifyAsync(order.MaTk, $"Yêu cầu hủy đơn #{id} bị từ chối", reason, ct);
        }
        await transaction.CommitAsync(ct);
        return Op.Ok(await AdminAsync(id, ct));
    }

    public async Task<OpResult<Fresh<MoneyEntry>>> RecordMoneyAsync(int id, MoneyForm form, int actor, CancellationToken ct)
    {
        var reference = (form.Reference ?? "").Trim();
        if (form.Kind is not (MoneyKinds.Receipt or MoneyKinds.Refund)) return Op.Invalid("Loại giao dịch chỉ nhận Receipt hoặc Refund.");
        if (form.Amount <= 0 || form.Amount != Math.Round(form.Amount)) return Op.Invalid("Số tiền phải là số nguyên VND lớn hơn 0.");
        if (reference.Length is 0 or > 50) return Op.Invalid("Mã tham chiếu 1–50 ký tự.");
        if (form.At.ToUniversalTime() > DateTime.UtcNow.AddMinutes(1)) return Op.Invalid("Thời điểm giao dịch không được ở tương lai.");
        // Khóa đơn trước khi đọc số đã thu: hai lần ghi đồng thời không cùng vượt "còn phải thu", và khoản tiền đi cùng nhật ký.
        await using var transaction = await repository.BeginAsync(ct);
        if (!await repository.LockOrderAsync(id, ct)) return Op.NotFound("Không tìm thấy đơn đặt.");
        var order = (await repository.GetAsync(id, ct))!;

        var duplicate = await DuplicateAsync(id, reference, ct);
        if (duplicate is not null) return duplicate;

        var money = ToMoney(order);
        if (form.Kind == MoneyKinds.Receipt)
        {
            if (order.TrangThai is not (OrderStatus.Pending or OrderStatus.Confirmed or OrderStatus.Expired))
                return Op.Conflict($"Đơn đang ở trạng thái \"{OrderStatus.Label(order.TrangThai)}\" nên không nhận thu được.");
            if (form.Amount > OrderRules.Outstanding(order.GiaChot, money)) return Op.Unprocessable("Số tiền thu vượt quá số còn phải thu.");
        }
        else if (form.Amount > OrderRules.Refundable(money)) return Op.Unprocessable("Số tiền hoàn vượt quá số còn có thể hoàn.");

        var entry = new Giaodichtien { MaDon = id, LoaiGd = form.Kind, SoTien = form.Amount, MaThamChieu = reference, ThoiDiem = form.At.ToUniversalTime(), MaTkghiNhan = actor };
        if (!await repository.AddMoneyAsync(entry, ct))
            return await DuplicateAsync(id, reference, ct) ?? Op.Conflict("Không ghi được giao dịch, vui lòng thử lại.");
        await administration.RecordAsync(new AuditEntry(actor, form.Kind == MoneyKinds.Receipt ? "Ghi nhận thu" : "Ghi nhận hoàn", MoneyType, entry.MaGd,
            $"Đơn #{id}; mã tham chiếu {reference}", null, new { entry.MaGd, entry.MaDon, entry.LoaiGd, entry.SoTien, entry.MaThamChieu }), ct);
        await transaction.CommitAsync(ct);
        return Op.Ok(new Fresh<MoneyEntry>(new MoneyEntry(entry.MaGd, entry.LoaiGd, entry.SoTien, entry.ThoiDiem, entry.MaThamChieu), true));
    }

    private async Task<OpResult<Fresh<MoneyEntry>>?> DuplicateAsync(int orderId, string reference, CancellationToken ct)
    {
        var existing = await repository.MoneyByReferenceAsync(reference, ct);
        if (existing is null) return null;
        return existing.MaDon == orderId
            ? Op.Ok(new Fresh<MoneyEntry>(new MoneyEntry(existing.MaGd, existing.LoaiGd, existing.SoTien, existing.ThoiDiem, existing.MaThamChieu), false))
            : Op.Conflict("Mã tham chiếu đã được dùng cho đơn khác.");
    }

    public async Task<OpResult<AdminBooking>> CompleteAsync(int id, int actor, CancellationToken ct)
    {
        var order = await repository.GetAsync(id, ct);
        if (order is null) return Op.NotFound("Không tìm thấy đơn đặt.");
        if (order.TrangThai != OrderStatus.Confirmed) return Op.Conflict("Chỉ hoàn thành được đơn đã xác nhận.");
        if (order.NgayDi is not { } tripDate || tripDate >= Today) return Op.Conflict("Đơn chưa khởi hành nên chưa hoàn thành được.");
        if (!await repository.MoveAsync(id, OrderStatus.Confirmed, OrderStatus.Completed, ct))
            return Op.Conflict("Đơn vừa được cập nhật bởi người khác; vui lòng tải lại.");
        await administration.RecordAsync(new AuditEntry(actor, "Hoàn thành đơn", OrderType, id, null, new { Status = OrderStatus.Confirmed }, new { Status = OrderStatus.Completed }), ct);
        return Op.Ok(await AdminAsync(id, ct));
    }

    /// <summary>Tác vụ nền: mỗi đơn quá hạn giữ chỗ và chưa đủ khoản cần thu chuyển Expired và trả chỗ đúng một lần, trong một giao dịch riêng.</summary>
    public async Task<int> ExpireHoldsAsync(DateTime now, CancellationToken ct)
    {
        var expired = 0;
        foreach (var (order, net) in await repository.ExpiredHoldsAsync(now, ct))
        {
            if (net >= (order.KhoanCanThu ?? 0) || order.MaLkh is not { } departureId) continue;
            try
            {
                await using var transaction = await repository.BeginAsync(ct);
                if (!await repository.MoveAsync(order.MaDon, OrderStatus.Pending, OrderStatus.Expired, ct)) continue;
                await catalog.ReleaseSeatsAsync(departureId, order.SoKhach, ct);
                await administration.RecordAsync(new AuditEntry(null, "Đơn hết hạn giữ chỗ", OrderType, order.MaDon, null,
                    new { Status = OrderStatus.Pending }, new { Status = OrderStatus.Expired }), ct);
                await administration.NotifyAsync(order.MaTk, $"Đơn #{order.MaDon} đã hết hạn giữ chỗ", "Đơn chưa đủ khoản cần thu trước hạn giữ chỗ nên đã hết hạn. Liên hệ nhân viên nếu bạn đã thanh toán.", ct);
                await transaction.CommitAsync(ct);
                expired++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Không xử lý được đơn hết hạn giữ chỗ {OrderId}.", order.MaDon);
            }
        }
        return expired;
    }

    // ---- Ánh xạ ----

    private static List<MoneyEntry> ToMoney(Dondat order) => order.Giaodichtien
        .Select(entry => new MoneyEntry(entry.MaGd, entry.LoaiGd, entry.SoTien, entry.ThoiDiem, entry.MaThamChieu)).ToList();

    private static List<string> Missing(Dondat order)
    {
        var missing = new List<string>();
        var money = ToMoney(order);
        var net = OrderRules.Refundable(money);
        var deposit = order.KhoanCanThu ?? 0;
        if (net < deposit) missing.Add($"còn thiếu {deposit - net:N0} VND so với khoản cần thu {deposit:N0} VND");
        return missing;
    }

    private async Task<AdminBooking> AdminAsync(int id, CancellationToken ct)
    {
        var order = (await repository.GetAsync(id, ct))!;
        var names = await administration.AccountNamesAsync([order.MaTk], ct);
        return ToAdmin(order, names, await bookings.TitlesAsync([order], ct));
    }

    private async Task<AdminBookingDetail> DetailAsync(Dondat order, CancellationToken ct)
    {
        var names = await administration.AccountNamesAsync([order.MaTk], ct);
        var titles = await bookings.TitlesAsync([order], ct);
        var missing = new List<string>();
        if (order.TrangThai is OrderStatus.Pending or OrderStatus.Expired)
        {
            var net = OrderRules.Refundable(ToMoney(order));
            var deposit = order.KhoanCanThu ?? 0;
            if (net < deposit) missing.Add($"Còn thiếu {deposit - net:N0} VND so với khoản cần thu {deposit:N0} VND");
            if (order.TrangThai == OrderStatus.Expired) missing.Add("Kiểm tra lại chỗ và giá");
        }
        return new AdminBookingDetail(ToAdmin(order, names, titles), await bookings.DetailAsync(order, ct), missing);
    }

    private static AdminBooking ToAdmin(Dondat order, IReadOnlyDictionary<int, string> names, IReadOnlyDictionary<int, string> titles) => new(order.MaDon, order.MaTk,
        names.GetValueOrDefault(order.MaTk, ""), order.MaLkh is null ? BookingSources.Quote : BookingSources.Departure, titles.GetValueOrDefault(order.MaDon, ""),
        order.NgayDat, order.SoKhach, order.GiaChot, OrderRules.Paid(ToMoney(order)), order.TrangThai, order.HanGiuCho);
}
