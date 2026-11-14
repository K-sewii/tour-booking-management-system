using System.Globalization;
using System.Text.Json;
using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence;

namespace DongNamTrip.Modules.Administration;

public sealed class AuditService(AuditRepository repository) : IAdministrationApi
{
    public async Task<OpResult<AdminAuditPage>> QueryScreenAsync(int? page, string? actorType, DateOnly? from, DateOnly? to, string? query, CancellationToken ct)
    {
        if (page is < 1 or > 1000000 || actorType is not (null or "" or "staff" or "customer" or "system") || from > to || from == DateOnly.MinValue || to == DateOnly.MaxValue)
            return Op.Invalid("Bộ lọc nhật ký không hợp lệ.");
        var start = from is { } f ? VnTime.ToUtc(f.ToDateTime(TimeOnly.MinValue)) : (DateTime?)null;
        var end = to is { } t ? VnTime.EndExclusiveUtc(t.ToDateTime(TimeOnly.MinValue)) : (DateTime?)null;
        return Op.Ok(await repository.AuditAsync(page ?? 1, actorType, start, end, query, ct));
    }
    public Task RecordAsync(AuditEntry entry, CancellationToken ct) => repository.AddAsync(
        entry.ActorId, entry.Action, entry.ObjectType, Id(entry.ObjectId),
        entry.Reason, Serialize(entry.Before), Serialize(entry.After), ct);

    public Task NotifyAsync(int accountId, string title, string body, CancellationToken ct) => repository.AddNoticeAsync(new AccountNotice
    {
        AccountId = accountId,
        Title = Cut(title, 160),
        Body = Cut(body, 500),
        CreatedAt = DateTime.UtcNow
    }, ct);

    public Task<List<AuditRow>> HistoryAsync(string objectType, object objectId, CancellationToken ct) =>
        repository.ByObjectAsync(objectType, Id(objectId), ct);

    public async Task<Dictionary<int, string>> AccountNamesAsync(IReadOnlyCollection<int> accountIds, CancellationToken ct) =>
        accountIds.Count == 0 ? [] : await repository.NamesAsync(accountIds.Distinct().ToArray(), ct);

    public async Task<OpResult<PagedResult<AuditRow>>> QueryAsync(AuditFilter filter, CancellationToken ct)
    {
        if ((filter.From is { } from && !VnTime.InRange(from)) || (filter.To is { } to && !VnTime.InRange(to)) || filter.From > filter.To)
            return Op.Invalid("Khoảng ngày không hợp lệ.");
        var page = Math.Clamp(filter.Page ?? 1, 1, Limits.MaxPage); // chặn (page - 1) * size tràn số nguyên
        var size = Math.Clamp(filter.Size ?? 20, 1, 100);
        // Ngày nhập theo giờ Việt Nam; ThoiDiem lưu UTC.
        var utc = filter with { From = filter.From is { } f ? VnTime.ToUtc(f) : null, To = filter.To is { } t ? VnTime.EndExclusiveUtc(t) : null };
        var (rows, total) = await repository.QueryAsync(utc, page, size, ct);
        return Op.Ok(new PagedResult<AuditRow>(rows, total, page, size));
    }

    private static string Id(object id) => Convert.ToString(id, CultureInfo.InvariantCulture) ?? "";
    private static string Cut(string value, int max) => value.Length <= max ? value : value[..max];
    private static string? Serialize(object? value) => value is null ? null : JsonSerializer.Serialize(value);
}
