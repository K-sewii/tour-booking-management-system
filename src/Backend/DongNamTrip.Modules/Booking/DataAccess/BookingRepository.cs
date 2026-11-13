using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DongNamTrip.Modules.Booking;

public enum RestoreOutcome { Restored, Lost, Duplicate }

/// <summary>Chỉ đọc/ghi DONDAT, GIAODICHTIEN và lựa chọn thanh toán của tài khoản.</summary>
public sealed class BookingRepository(DongNamTripDbContext db)
{
    public Task<List<Dondat>> OrdersAsync(int accountId, CancellationToken ct) =>
        db.Dondat.AsNoTracking().Where(item => item.MaTk == accountId)
            .Include(item => item.Giaodichtien).OrderByDescending(item => item.NgayDat).ThenByDescending(item => item.MaDon).ToListAsync(ct);

    public Task<Dondat?> OrderAsync(int accountId, int orderId, CancellationToken ct) =>
        db.Dondat.AsNoTracking().Include(order => order.Giaodichtien)
            .SingleOrDefaultAsync(order => order.MaDon == orderId && order.MaTk == accountId, ct);

    public Task<AccountPaymentChoice?> PaymentChoiceAsync(int accountId, CancellationToken ct) =>
        db.AccountPaymentChoices.AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == accountId, ct);

    /// <summary>Tạo hoặc cập nhật; hai yêu cầu đồng thời tạo lần đầu không gây lỗi 500.</summary>
    public async Task<AccountPaymentChoice> SavePaymentChoiceAsync(int accountId, string method, DateTime now, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            var choice = await db.AccountPaymentChoices.SingleOrDefaultAsync(x => x.AccountId == accountId, ct);
            if (choice is null)
            {
                choice = new() { AccountId = accountId };
                db.AccountPaymentChoices.Add(choice);
            }
            choice.MethodCode = method;
            choice.UpdatedAt = now;
            try { await db.SaveChangesAsync(ct); return choice; }
            catch (DbUpdateException exception) when (attempt == 0 && DbErrors.IsDuplicateKey(exception)) { db.ChangeTracker.Clear(); }
        }
    }

    // ---- Vòng đời đơn (UC11-13, UC19, UC20) ----

    public Task<IDbContextTransaction> BeginAsync(CancellationToken ct) => db.Database.BeginTransactionAsync(ct);

    /// <summary>Thêm đơn; trả null khi vi phạm chỉ mục duy nhất (đơn Pending trùng, hoặc báo giá đã có đơn) và đã dọn tracker.</summary>
    public async Task<int?> AddOrderAsync(Dondat order, CancellationToken ct)
    {
        db.Dondat.Add(order);
        try { await db.SaveChangesAsync(ct); return order.MaDon; }
        catch (DbUpdateException exception) when (DbErrors.IsDuplicateKey(exception)) { db.ChangeTracker.Clear(); return null; }
    }

    /// <summary>Đơn Pending của tài khoản cho lịch khởi hành.</summary>
    public Task<Dondat?> OpenOrderAsync(int accountId, int departureId, CancellationToken ct) =>
        db.Dondat.AsNoTracking().Include(item => item.Giaodichtien)
            .FirstOrDefaultAsync(item => item.MaTk == accountId && item.MaLkh == departureId && item.TrangThai == OrderStatus.Pending, ct);

    public Task<Dondat?> ByQuoteAsync(int quoteId, CancellationToken ct) =>
        db.Dondat.AsNoTracking().FirstOrDefaultAsync(item => item.MaBg == quoteId, ct);

    /// <summary>Khóa dòng DONDAT bằng UPDLOCK trong giao dịch của người gọi (tuần tự hóa ghi tiền trên cùng một đơn); false nếu không có đơn.</summary>
    public async Task<bool> LockOrderAsync(int id, CancellationToken ct) =>
        await db.Dondat.FromSqlInterpolated($"SELECT * FROM dbo.DONDAT WITH (UPDLOCK, ROWLOCK) WHERE MaDon = {id}").AsNoTracking().AnyAsync(ct);

    public Task<Dondat?> GetAsync(int id, CancellationToken ct) =>
        db.Dondat.AsNoTracking().Include(item => item.Giaodichtien).FirstOrDefaultAsync(item => item.MaDon == id, ct);

    /// <summary>UPDATE ... WHERE MaDon = @id AND TrangThai = @from; false nghĩa là đơn đã bị người khác đổi trước.</summary>
    public async Task<bool> MoveAsync(int id, string from, string to, CancellationToken ct) =>
        await db.Dondat.Where(item => item.MaDon == id && item.TrangThai == from)
            .ExecuteUpdateAsync(set => set.SetProperty(item => item.TrangThai, to), ct) == 1;

    public async Task<bool> SetCancelAsync(int id, string reason, string previous, CancellationToken ct) =>
        await db.Dondat.Where(item => item.MaDon == id)
            .ExecuteUpdateAsync(set => set.SetProperty(item => item.LyDoHuy, reason).SetProperty(item => item.TrangThaiTruocHuy, previous), ct) == 1;

    /// <summary>Từ chối hủy: CancelRequested trở về trạng thái cũ và xóa TrangThaiTruocHuy. Đặt lại Pending có thể trùng UX_DONDAT_Pending_TK_LKH.</summary>
    public async Task<RestoreOutcome> RestoreFromCancelAsync(int id, string previous, CancellationToken ct)
    {
        try
        {
            return await db.Dondat.Where(item => item.MaDon == id && item.TrangThai == OrderStatus.CancelRequested)
                .ExecuteUpdateAsync(set => set.SetProperty(item => item.TrangThai, previous).SetProperty(item => item.TrangThaiTruocHuy, (string?)null), ct) == 1
                ? RestoreOutcome.Restored : RestoreOutcome.Lost;
        }
        catch (Exception exception) when (DbErrors.IsDuplicateKey(exception)) { return RestoreOutcome.Duplicate; }
    }

    public async Task<bool> UpdatePriceAsync(int id, decimal price, decimal deposit, CancellationToken ct) =>
        await db.Dondat.Where(item => item.MaDon == id)
            .ExecuteUpdateAsync(set => set.SetProperty(item => item.GiaChot, price).SetProperty(item => item.KhoanCanThu, (decimal?)deposit), ct) == 1;

    /// <summary>Ghi giao dịch tiền; false khi trùng MaThamChieu (UQ_GIAODICHTIEN_MaThamChieu).</summary>
    public async Task<bool> AddMoneyAsync(Giaodichtien entry, CancellationToken ct)
    {
        db.Giaodichtien.Add(entry);
        try { await db.SaveChangesAsync(ct); return true; }
        catch (DbUpdateException exception) when (DbErrors.IsDuplicateKey(exception)) { db.ChangeTracker.Clear(); return false; }
    }

    public Task<List<Giaodichtien>> MoneyAsync(int orderId, CancellationToken ct) =>
        db.Giaodichtien.AsNoTracking().Where(item => item.MaDon == orderId).OrderByDescending(item => item.ThoiDiem).ToListAsync(ct);

    public Task<Giaodichtien?> MoneyByReferenceAsync(string reference, CancellationToken ct) =>
        db.Giaodichtien.AsNoTracking().FirstOrDefaultAsync(item => item.MaThamChieu == reference, ct);

    /// <summary>Đơn Pending tour có sẵn đã quá hạn giữ chỗ, kèm Paid - Refunded; service giữ lại đơn đã đủ khoản cần thu.</summary>
    public async Task<List<(Dondat Order, decimal Net)>> ExpiredHoldsAsync(DateTime now, CancellationToken ct)
    {
        var rows = await db.Dondat.AsNoTracking().Include(item => item.Giaodichtien)
            .Where(item => item.TrangThai == OrderStatus.Pending && item.MaLkh != null && item.HanGiuCho != null && item.HanGiuCho < now).ToListAsync(ct);
        return rows.Select(row => (row, row.Giaodichtien.Where(entry => entry.LoaiGd == MoneyKinds.Receipt).Sum(entry => entry.SoTien)
            - row.Giaodichtien.Where(entry => entry.LoaiGd == MoneyKinds.Refund).Sum(entry => entry.SoTien))).ToList();
    }

    private static DateTime ToUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();

    public async Task<(List<Dondat> Rows, int Total)> ListAsync(BookingFilter filter, DateOnly today, int skip, int take, CancellationToken ct)
    {
        var query = db.Dondat.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(item => item.TrangThai == filter.Status);
        // NgayDat lưu giờ UTC, bộ lọc nhập theo giờ máy chủ; To không kèm giờ nghĩa là hết ngày đó.
        if (filter.From is { } from) { var start = ToUtc(from); query = query.Where(item => item.NgayDat >= start); }
        if (filter.To is { } to) { var end = ToUtc(to.TimeOfDay == TimeSpan.Zero ? to.AddDays(1) : to.AddTicks(1)); query = query.Where(item => item.NgayDat < end); }
        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var q = filter.Q.Trim();
            query = int.TryParse(q, out var id) ? query.Where(item => item.MaDon == id || (item.LienHe != null && item.LienHe.Contains(q)))
                : query.Where(item => item.LienHe != null && item.LienHe.Contains(q));
        }
        if (filter.Overdue == true) query = query.Where(item => item.TrangThai == OrderStatus.Confirmed && item.NgayDi != null && item.NgayDi < today);
        var total = await query.CountAsync(ct);
        // Đơn Chờ xử lý: hạn giữ chỗ gần nhất trước (đơn tour riêng không có hạn nên xuống cuối); còn lại: mới nhất trước.
        var ordered = filter.Status == OrderStatus.Pending
            ? query.OrderBy(item => item.HanGiuCho == null).ThenBy(item => item.HanGiuCho).ThenBy(item => item.MaDon)
            : query.OrderByDescending(item => item.NgayDat).ThenByDescending(item => item.MaDon);
        var rows = await ordered.Include(item => item.Giaodichtien).Skip(skip).Take(take).ToListAsync(ct);
        return (rows, total);
    }
}
