using System.Text.Json;
using DongNamTrip.Contracts;
using DongNamTrip.Modules.Catalog;

namespace DongNamTrip.Modules.Itinerary;

/// <summary>Phiên bản lịch trình cho báo giá (QT05): ảnh chụp không bao giờ bị sửa.</summary>
public sealed class ItineraryVersionService(ItineraryRepository repository, ICatalogApi catalog, ScheduleChecker checker)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record SnapshotSlot(int DestinationId, int SlotId, string? Note);
    private sealed record Snapshot(string Name, DateOnly StartDate, int Guests, int[] DestinationIds, int[] Days, SnapshotSlot[] Slots);

    public async Task<OpResult<ItineraryVersionRef>> CreateVersionAsync(int itineraryId, int accountId, CancellationToken ct)
    {
        // Khóa trước rồi mới đọc, để ảnh chụp là trạng thái cuối cùng của lịch trình.
        if (!await repository.LockAsync(itineraryId, accountId, ct)) return Op.NotFound("Không tìm thấy lịch trình của bạn.");
        var row = await repository.GetAsync(itineraryId, accountId, ct);
        if (row is null) return Op.NotFound("Không tìm thấy lịch trình của bạn.");
        var snapshot = new Snapshot(row.Name, row.StartDate, row.Guests,
            row.Stops.Select(stop => stop.DestinationId).ToArray(), row.Stops.Select(stop => stop.Day).ToArray(),
            row.Stops.SelectMany(stop => stop.SlotIds.Select(slot => new SnapshotSlot(stop.DestinationId, slot, stop.Note))).ToArray());
        return Op.Ok(await repository.AddVersionAsync(itineraryId, JsonSerializer.Serialize(snapshot, Json), ct));
    }

    /// <summary>Ngày đi và số khách lấy từ ảnh chụp của phiên bản (QT05), không phải từ lịch trình hiện tại mà khách có thể đã sửa sau khi gửi yêu cầu.</summary>
    public async Task<ItineraryVersion?> VersionAsync(int versionId, CancellationToken ct)
    {
        var version = await repository.VersionAsync(versionId, ct);
        var snapshot = version is null ? null : JsonSerializer.Deserialize<Snapshot>(version.ContentJson, Json);
        return snapshot is null ? version : version! with { StartDate = snapshot.StartDate, Guests = snapshot.Guests };
    }

    public async Task<List<ProgramStop>> ProgramAsync(int versionId, CancellationToken ct)
    {
        var version = await repository.VersionAsync(versionId, ct);
        var snapshot = version is null ? null : JsonSerializer.Deserialize<Snapshot>(version.ContentJson, Json);
        var ids = snapshot?.DestinationIds ?? [];
        if (ids.Length == 0) return [];
        var names = await catalog.PlaceNamesAsync(ids, ct);
        var days = snapshot!.Days is { Length: > 0 } && snapshot.Days.Length == ids.Length ? snapshot.Days : ids.Select(_ => 1).ToArray();
        return ids.Select((id, index) => (Day: days[index], Index: index, Place: names.GetValueOrDefault(id, $"Địa điểm #{id}")))
            .OrderBy(stop => stop.Day).ThenBy(stop => stop.Index).Select(stop => new ProgramStop(stop.Day, stop.Place)).ToList();
    }

    public async Task<ItineraryCheck> CheckVersionAsync(int versionId, CancellationToken ct)
    {
        var version = await repository.VersionAsync(versionId, ct);
        if (version is null) return new ItineraryCheck(false, false, []);
        var snapshot = JsonSerializer.Deserialize<Snapshot>(version.ContentJson, Json);
        if (snapshot is null) return new ItineraryCheck(false, false, []);
        var ids = snapshot.DestinationIds ?? [];
        var days = snapshot.Days is { Length: > 0 } && snapshot.Days.Length == ids.Length ? snapshot.Days : ids.Select(_ => 1).ToArray();
        var picks = (snapshot.Slots ?? []).Select(slot => new SlotPick(slot.DestinationId, slot.SlotId, slot.Note)).ToList();
        var places = await catalog.DestinationsAsync(ids, ct);
        var hours = await catalog.HoursAsync(ids, ct);
        var visit = places.ToDictionary(place => place.Id, place => place.VisitMinutes);
        var stops = ids.Select((id, index) => new StopPlan(id, days[index], index + 1, visit.GetValueOrDefault(id),
            hours.GetValueOrDefault(id)?.Opens, hours.GetValueOrDefault(id)?.Closes)).ToList();
        var slots = await catalog.SlotDetailsAsync(picks.Select(pick => pick.SlotId).Distinct().ToArray(), ct);
        return checker.Check(snapshot.StartDate, stops, slots, picks);
    }
}
