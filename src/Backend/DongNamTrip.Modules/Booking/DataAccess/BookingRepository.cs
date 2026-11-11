using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Booking;

/// <summary>Chỉ đọc/ghi DONDAT, GIAODICHTIEN và lựa chọn thanh toán của tài khoản.</summary>
public sealed class BookingRepository(DongNamTripDbContext db)
{
    public async Task<AdminStatistics> StatisticsAsync(DateOnly from, DateOnly to, string? status, CancellationToken ct)
    {
        // SQL timestamps are UTC; business dates are Vietnam time (UTC+07:00).
        var start = from.ToDateTime(TimeOnly.MinValue).AddHours(-7);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-7);
        var orders = db.Dondat.AsNoTracking().Where(x => x.NgayDat >= start && x.NgayDat < end);
        var statuses = await db.Dondat.AsNoTracking().Select(x => x.TrangThai).Distinct().OrderBy(x => x).ToListAsync(ct);
        if (!string.IsNullOrWhiteSpace(status)) orders = orders.Where(x => x.TrangThai == status);
        var grouped = await orders.GroupBy(x => x.TrangThai).Select(g => new { Status = g.Key, Count = g.Count(), Value = g.Sum(x => x.GiaChot) }).OrderBy(x => x.Status).ToListAsync(ct);
        var rows = grouped.Select(x => new AdminStatisticsRow(x.Status, x.Count, x.Value)).ToList();
        // No contract defines transaction types, so totals cannot yet be classified.
        return new AdminStatistics(from, to, rows.Sum(x => x.OrderValue), null, null, null, rows, statuses);
    }
    public Task<List<Dondat>> OrdersAsync(int accountId, CancellationToken ct) =>
        db.Dondat.AsNoTracking().Where(item => item.MaTk == accountId)
            .Include(item => item.Giaodichtien).OrderByDescending(item => item.NgayDat).ToListAsync(ct);

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
}
