using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Modules.Booking;

/// <summary>Chỉ đọc/ghi DONDAT, GIAODICHTIEN và lựa chọn thanh toán của tài khoản.</summary>
public sealed class BookingRepository(DongNamTripDbContext db)
{
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
