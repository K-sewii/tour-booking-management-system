using DongNamTrip.Modules.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Modules.Administration;

/// <summary>Chỉ đọc/ghi các bảng khu tài khoản (yêu thích, lịch sử, thông báo, tùy chọn); không chứa quy tắc nghiệp vụ.</summary>
public sealed class AccountAreaRepository(DongNamTripDbContext db)
{
    public Task<List<AccountFavorite>> FavoritesAsync(int accountId, CancellationToken ct) =>
        db.AccountFavorites.AsNoTracking().Where(x => x.AccountId == accountId).OrderByDescending(x => x.SavedAt).ToListAsync(ct);

    public Task<List<AccountActivity>> RecentActivityAsync(int accountId, int take, CancellationToken ct) =>
        db.AccountActivities.AsNoTracking().Where(x => x.AccountId == accountId)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Take(take).ToListAsync(ct);

    /// <summary>Thêm nếu chưa có; hai yêu cầu đồng thời cùng thêm một mục vẫn thành công (idempotent).</summary>
    public async Task AddFavoriteAsync(int accountId, string kind, int itemId, DateTime savedAt, CancellationToken ct)
    {
        if (await db.AccountFavorites.AnyAsync(x => x.AccountId == accountId && x.Kind == kind && x.ItemId == itemId, ct))
            return;
        db.AccountFavorites.Add(new() { AccountId = accountId, Kind = kind, ItemId = itemId, SavedAt = savedAt });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException exception) when (DbErrors.IsDuplicateKey(exception)) { db.ChangeTracker.Clear(); }
    }

    public Task RemoveFavoriteAsync(int accountId, string kind, int itemId, CancellationToken ct) =>
        db.AccountFavorites.Where(x => x.AccountId == accountId && x.Kind == kind && x.ItemId == itemId).ExecuteDeleteAsync(ct);

    public async Task AddActivityAsync(AccountActivity activity, CancellationToken ct)
    {
        db.AccountActivities.Add(activity);
        await db.SaveChangesAsync(ct);
    }

    public Task RemoveActivityAsync(int accountId, long id, CancellationToken ct) =>
        db.AccountActivities.Where(x => x.Id == id && x.AccountId == accountId).ExecuteDeleteAsync(ct);

    /// <summary><paramref name="searches"/> = true xóa các lượt tìm; false xóa các mục đã xem (mọi loại khác search).</summary>
    public Task ClearActivityAsync(int accountId, bool searches, string searchKind, CancellationToken ct) =>
        db.AccountActivities.Where(x => x.AccountId == accountId && (searches ? x.Kind == searchKind : x.Kind != searchKind)).ExecuteDeleteAsync(ct);

    public Task<List<AccountNotice>> NoticesAsync(int accountId, CancellationToken ct) =>
        db.AccountNotices.AsNoTracking().Where(x => x.AccountId == accountId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<bool> MarkNoticeReadAsync(int accountId, long id, DateTime readAt, CancellationToken ct) =>
        await db.AccountNotices.Where(x => x.Id == id && x.AccountId == accountId && x.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ReadAt, readAt), ct) > 0;

    /// <summary>Null nếu tài khoản chưa lưu tùy chọn nào.</summary>
    public async Task<bool?> SecurityNoticesAsync(int accountId, CancellationToken ct) =>
        (await db.AccountPreferences.AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == accountId, ct))?.SecurityNotices;

    public async Task SaveSecurityNoticesAsync(int accountId, bool value, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            var current = await db.AccountPreferences.SingleOrDefaultAsync(x => x.AccountId == accountId, ct);
            if (current is null) db.AccountPreferences.Add(new() { AccountId = accountId, SecurityNotices = value });
            else current.SecurityNotices = value;
            try { await db.SaveChangesAsync(ct); return; }
            catch (DbUpdateException exception) when (attempt == 0 && DbErrors.IsDuplicateKey(exception)) { db.ChangeTracker.Clear(); }
        }
    }
}
