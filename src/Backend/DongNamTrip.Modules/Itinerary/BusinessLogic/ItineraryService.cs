using DongNamTrip.Contracts;
using DongNamTrip.Modules.Catalog;

namespace DongNamTrip.Modules.Itinerary;

public sealed class ItineraryService(ItineraryRepository repository, ICatalogApi catalog) : IItineraryApi
{
    public Task<PlannerCatalog> CatalogAsync(CancellationToken ct) => catalog.PlannerCatalogAsync(ct);
    public Task<ItineraryStopDraft[]?> TourTemplateAsync(int tourId, CancellationToken ct) => catalog.TourTemplateAsync(tourId, ct);

    public async Task<ItineraryPreview> PreviewAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        var places = ids.Count == 0 ? [] : await catalog.DestinationsAsync([.. ids.Distinct().Take(Limits.MaxPlaces)], ct);
        return new(places, places.Sum(place => place.VisitMinutes));
    }

    public async Task<List<Contracts.Itinerary>> ListAsync(int accountId, CancellationToken ct)
    {
        var rows = await repository.ListAsync(accountId, ct);
        var result = new List<Contracts.Itinerary>(rows.Count);
        foreach (var row in rows) result.Add(await BuildAsync(row, ct));
        return result;
    }

    public async Task<Contracts.Itinerary?> GetAsync(int id, int accountId, CancellationToken ct) =>
        await repository.GetAsync(id, accountId, ct) is { } row ? await BuildAsync(row, ct) : null;

    public async Task<OpResult<Contracts.Itinerary>> SaveAsync(int? id, int accountId, ItineraryDraft draft, CancellationToken ct)
    {
        var stops = NormalizeStops(draft);
        if (stops is null || string.IsNullOrWhiteSpace(draft.Name) || draft.StartDate < DateOnly.FromDateTime(DateTime.Today))
            return Op.Invalid("Lịch trình cần tên, ngày hợp lệ, 1–12 điểm khác nhau và thứ tự hợp lệ.");
        var places = await catalog.DestinationsAsync(stops.Select(stop => stop.DestinationId).ToArray(), ct);
        if (places.Count != stops.Length) return Op.Invalid("Địa điểm không tồn tại hoặc chưa công bố.");
        var slotIds = stops.SelectMany(stop => stop.SlotIds).ToArray();
        if (slotIds.Length != slotIds.Distinct().Count()) return Op.Invalid("Một suất chỉ được chọn một lần.");
        var slots = await catalog.SlotsAsync(slotIds, ct);
        if (slots.Count != slotIds.Length || slots.Any(slot => slot.Status != "Published" ||
            !stops.Any(stop => stop.DestinationId == slot.DestinationId && stop.SlotIds.Contains(slot.Id)
                && slot.Date == draft.StartDate.AddDays(stop.Day - 1))))
            return Op.Invalid("Suất phải đã công bố, thuộc điểm đã chọn và đúng ngày dự kiến.");
        var savedId = await repository.SaveAsync(id, accountId, draft, stops, ct);
        if (savedId is null) return Op.NotFound("Không tìm thấy lịch trình của bạn.");
        return Op.Ok((await GetAsync(savedId.Value, accountId, ct))!);
    }

    private async Task<Contracts.Itinerary> BuildAsync(ItineraryData row, CancellationToken ct)
    {
        var places = await catalog.DestinationsAsync(row.Stops.Select(stop => stop.DestinationId).ToArray(), ct);
        var byId = places.ToDictionary(place => place.Id);
        var slotIds = row.Stops.SelectMany(stop => stop.SlotIds).Distinct().ToArray();
        var slots = await catalog.SlotsAsync(slotIds, ct);
        var bySlot = slots.ToDictionary(slot => slot.Id);
        var warnings = new List<string>();
        foreach (var stop in row.Stops)
        {
            foreach (var slotId in stop.SlotIds)
            {
                if (!bySlot.TryGetValue(slotId, out var slot)) { warnings.Add($"Suất #{slotId} không còn tồn tại."); continue; }
                if (slot.Date != row.StartDate.AddDays(stop.Day - 1)) warnings.Add($"Suất #{slotId} không đúng ngày thứ {stop.Day}.");
                if (slot.Status != "Published") warnings.Add($"Suất #{slotId} đã ngừng công bố.");
            }
        }
        foreach (var day in slots.GroupBy(slot => slot.Date))
        {
            var entries = day.ToArray();
            for (var i = 0; i < entries.Length; i++)
                for (var j = i + 1; j < entries.Length; j++)
                    if (entries[i].StartsAt < entries[j].EndsAt && entries[j].StartsAt < entries[i].EndsAt)
                        warnings.Add($"Các suất #{entries[i].Id} và #{entries[j].Id} trùng giờ.");
        }
        if (row.Stops.GroupBy(stop => stop.Day).Any(group => group.Count() > 1))
            warnings.Add("Chưa có dữ liệu thời gian di chuyển giữa các điểm; cần điều hành xác minh.");
        return new(row.Id, row.UserId, row.Name, row.StartDate, row.Guests,
            row.Stops.Select(stop => stop.DestinationId).ToArray(),
            row.Stops.Where(stop => byId.ContainsKey(stop.DestinationId)).Select(stop => byId[stop.DestinationId].Name).ToArray(),
            places.Sum(place => place.VisitMinutes))
        { DeparturePoint = row.DeparturePoint, Transport = row.Transport, Budget = row.Budget, UpdatedAt = row.UpdatedAt, Stops = row.Stops, Warnings = warnings.ToArray() };
    }

    private static ItineraryStopDraft[]? NormalizeStops(ItineraryDraft draft)
    {
        var stops = draft.Stops is { Length: > 0 } ? draft.Stops :
            (draft.DestinationIds ?? []).Select((placeId, index) => new ItineraryStopDraft { DestinationId = placeId, Day = 1, Order = index + 1 }).ToArray();
        if (stops.Length is < 1 or > Limits.MaxPlaces || stops.Any(stop => stop.DestinationId < 1 || stop.Day is < 1 or > 30 ||
            stop.Order is < 1 or > Limits.MaxPlaces || stop.Note?.Length > 500 || stop.SlotIds is null || stop.SlotIds.Length > 20)
            || stops.Select(stop => stop.DestinationId).Distinct().Count() != stops.Length
            || stops.GroupBy(stop => (stop.Day, stop.Order)).Any(group => group.Count() > 1)) return null;
        return stops.OrderBy(stop => stop.Day).ThenBy(stop => stop.Order).ToArray();
    }
}
