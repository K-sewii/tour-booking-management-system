using DongNamTrip.Contracts;
using DongNamTrip.Modules.Catalog;

namespace DongNamTrip.Modules.Administration;

/// <summary>Quy tắc khu tài khoản: yêu thích, lịch sử, thông báo, tùy chọn. Tên mục lấy từ Catalog qua <see cref="ICatalogApi"/>.</summary>
public sealed class AccountAreaService(AccountAreaRepository repository, ICatalogApi catalog)
{
    private const int ActivityWindow = 200;
    private const int QueryMax = 200;

    public async Task<AccountCollections> CollectionsAsync(int accountId, CancellationToken ct)
    {
        var favorites = await repository.FavoritesAsync(accountId, ct);
        var activity = await repository.RecentActivityAsync(accountId, ActivityWindow, ct);
        int[] Ids(string kind) => [.. favorites.Where(x => x.Kind == kind).Select(x => x.ItemId)
            .Concat(activity.Where(x => x.Kind == kind && x.ItemId != null).Select(x => x.ItemId!.Value)).Distinct()];
        var tourNames = await catalog.NamesAsync(ItemKinds.Tour, Ids(ItemKinds.Tour), ct);
        var placeNames = await catalog.NamesAsync(ItemKinds.Destination, Ids(ItemKinds.Destination), ct);
        string? Name(string kind, int? id) => id is not null
            && (kind == ItemKinds.Tour ? tourNames : kind == ItemKinds.Destination ? placeNames : []).TryGetValue(id.Value, out var name) ? name : null;

        return new AccountCollections(
            favorites.Select(x => new AccountCollectionItem(x.Kind, x.ItemId, Name(x.Kind, x.ItemId), x.SavedAt, Name(x.Kind, x.ItemId) is null)).ToArray(),
            activity.Where(x => x.Kind == ActivityKinds.Search).Select(x => new AccountActivityEntry(x.Id, x.Kind, null, x.Query, null, x.CreatedAt, false)).ToArray(),
            activity.Where(x => x.Kind != ActivityKinds.Search).Select(x => new AccountActivityEntry(x.Id, x.Kind, x.ItemId, null, Name(x.Kind, x.ItemId), x.CreatedAt, Name(x.Kind, x.ItemId) is null)).ToArray());
    }

    /// <returns>False nếu mục không tồn tại hoặc không còn hiển thị trong Catalog.</returns>
    public async Task<bool> AddFavoriteAsync(int accountId, string kind, int itemId, CancellationToken ct)
    {
        if (!await catalog.ExistsAsync(kind, itemId, ct)) return false;
        await repository.AddFavoriteAsync(accountId, kind, itemId, DateTime.UtcNow, ct);
        return true;
    }

    public Task RemoveFavoriteAsync(int accountId, string kind, int itemId, CancellationToken ct) =>
        repository.RemoveFavoriteAsync(accountId, kind, itemId, ct);

    /// <returns>False nếu bản ghi không hợp lệ: tìm kiếm cần từ khóa (không kèm mục), mục đã xem cần mục tồn tại (không kèm từ khóa).</returns>
    public async Task<bool> RecordActivityAsync(int accountId, AccountActivityDraft draft, CancellationToken ct)
    {
        var query = draft.Query?.Trim();
        var valid = draft.Kind == ActivityKinds.Search
            ? !string.IsNullOrEmpty(query) && query.Length <= QueryMax && draft.ItemId is null
            : draft.ItemId is not null && query is null && await catalog.ExistsAsync(draft.Kind, draft.ItemId.Value, ct);
        if (!valid) return false;
        await repository.AddActivityAsync(new() { AccountId = accountId, Kind = draft.Kind, ItemId = draft.ItemId, Query = query, CreatedAt = DateTime.UtcNow }, ct);
        return true;
    }

    public Task RemoveActivityAsync(int accountId, long id, CancellationToken ct) => repository.RemoveActivityAsync(accountId, id, ct);

    /// <returns>False nếu <paramref name="scope"/> không thuộc <see cref="ActivityScopes"/>.</returns>
    public async Task<bool> ClearActivityAsync(int accountId, string scope, CancellationToken ct)
    {
        if (scope is not (ActivityScopes.Search or ActivityScopes.Viewed)) return false;
        await repository.ClearActivityAsync(accountId, scope == ActivityScopes.Search, ActivityKinds.Search, ct);
        return true;
    }

    public async Task<List<AccountNoticeEntry>> NoticesAsync(int accountId, CancellationToken ct) =>
        (await repository.NoticesAsync(accountId, ct)).Select(x => new AccountNoticeEntry(x.Id, x.Title, x.Body, x.CreatedAt, x.ReadAt)).ToList();

    /// <returns>False nếu thông báo không thuộc tài khoản hoặc đã đọc.</returns>
    public Task<bool> MarkNoticeReadAsync(int accountId, long id, CancellationToken ct) =>
        repository.MarkNoticeReadAsync(accountId, id, DateTime.UtcNow, ct);

    /// <summary>Mặc định bật thông báo bảo mật khi chưa lưu tùy chọn.</summary>
    public async Task<AccountPreferenceDto> PreferenceAsync(int accountId, CancellationToken ct) =>
        new(await repository.SecurityNoticesAsync(accountId, ct) ?? true);

    public async Task<AccountPreferenceDto> SavePreferenceAsync(int accountId, AccountPreferenceDto preference, CancellationToken ct)
    {
        await repository.SaveSecurityNoticesAsync(accountId, preference.SecurityNotices, ct);
        return preference;
    }
}
