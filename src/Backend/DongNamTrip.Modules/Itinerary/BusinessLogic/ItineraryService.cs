using DongNamTrip.Contracts;
using DongNamTrip.Modules.Catalog;

namespace DongNamTrip.Modules.Itinerary;

public sealed class ItineraryService(ItineraryRepository repository, ICatalogApi catalog, ScheduleChecker checker, ItineraryVersionService versions) : IItineraryApi
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
        if (slots.Count != slotIds.Length || slots.Any(slot => slot.Status != ContentStatus.Published ||
            !stops.Any(stop => stop.DestinationId == slot.DestinationId && stop.SlotIds.Contains(slot.Id)
                && slot.Date == draft.StartDate.AddDays(stop.Day - 1))))
            return Op.Invalid("Suất phải đã công bố, thuộc điểm đã chọn và đúng ngày dự kiến.");
        var savedId = await repository.SaveAsync(id, accountId, draft, stops, ct);
        if (savedId is null) return Op.NotFound("Không tìm thấy lịch trình của bạn.");
        return Op.Ok((await GetAsync(savedId.Value, accountId, ct))!);
    }

    public Task<OpResult<ItineraryVersionRef>> CreateVersionAsync(int itineraryId, int accountId, CancellationToken ct) => versions.CreateVersionAsync(itineraryId, accountId, ct);
    public Task<ItineraryVersion?> VersionAsync(int versionId, CancellationToken ct) => versions.VersionAsync(versionId, ct);
    public Task<ItineraryCheck> CheckVersionAsync(int versionId, CancellationToken ct) => versions.CheckVersionAsync(versionId, ct);
    public Task<List<ProgramStop>> ProgramAsync(int versionId, CancellationToken ct) => versions.ProgramAsync(versionId, ct);

    /// <summary>Kiểm tra lịch trình đã lưu của chính chủ (UC07); 404 nếu không phải của khách.</summary>
    public async Task<OpResult<ItineraryCheck>> CheckAsync(int accountId, int id, CancellationToken ct)
    {
        var row = await repository.GetAsync(id, accountId, ct);
        if (row is null) return Op.NotFound("Không tìm thấy lịch trình của bạn.");
        var places = await catalog.DestinationsAsync(row.Stops.Select(stop => stop.DestinationId).ToArray(), ct);
        return Op.Ok(await CheckStopsAsync(row.StartDate, row.Stops, [], places, ct));
    }

    /// <summary>Kiểm tra nháp tạm của khách chưa đăng nhập (UC07); chỉ cảnh báo, không từ chối chọn trùng.</summary>
    public async Task<OpResult<ItineraryCheck>> CheckDraftAsync(DraftCheckRequest request, CancellationToken ct)
    {
        var stops = NormalizeStops(request.Draft);
        if (stops is null) return Op.Invalid("Nháp cần 1–12 điểm khác nhau và thứ tự hợp lệ.");
        var places = await catalog.DestinationsAsync(stops.Select(stop => stop.DestinationId).ToArray(), ct);
        if (places.Count != stops.Length) return Op.Invalid("Địa điểm không tồn tại hoặc chưa công bố.");
        return Op.Ok(await CheckStopsAsync(request.Draft.StartDate, stops, request.Picks ?? [], places, ct));
    }

    public async Task<OpResult<bool>> DeleteAsync(int accountId, int id, CancellationToken ct)
    {
        if (await repository.GetAsync(id, accountId, ct) is null) return Op.NotFound("Không tìm thấy lịch trình của bạn.");
        if (await repository.HasRequestsAsync(id, ct)) return Op.Conflict("Lịch trình đã có yêu cầu tour riêng nên không xóa được.");
        return await repository.DeleteAsync(id, accountId, ct) ? Op.Ok(true) : Op.NotFound("Không tìm thấy lịch trình của bạn.");
    }

    private async Task<ItineraryCheck> CheckStopsAsync(DateOnly startDate, IReadOnlyList<ItineraryStopDraft> stops, IReadOnlyList<SlotPick> extraPicks,
        IReadOnlyList<Destination> places, CancellationToken ct)
    {
        var hours = await catalog.HoursAsync(stops.Select(stop => stop.DestinationId).ToArray(), ct);
        var visit = places.ToDictionary(place => place.Id, place => place.VisitMinutes);
        var plans = stops.Select(stop => new StopPlan(stop.DestinationId, stop.Day, stop.Order, visit.GetValueOrDefault(stop.DestinationId),
            hours.GetValueOrDefault(stop.DestinationId)?.Opens, hours.GetValueOrDefault(stop.DestinationId)?.Closes)).ToList();
        var picks = stops.SelectMany(stop => (stop.SlotIds ?? []).Select(slot => new SlotPick(stop.DestinationId, slot, stop.Note))).Concat(extraPicks).ToList();
        var slots = await catalog.SlotDetailsAsync(picks.Select(pick => pick.SlotId).Distinct().ToArray(), ct);
        return checker.Check(startDate, plans, slots, picks);
    }

    private async Task<Contracts.Itinerary> BuildAsync(ItineraryData row, CancellationToken ct)
    {
        var places = await catalog.DestinationsAsync(row.Stops.Select(stop => stop.DestinationId).ToArray(), ct);
        var byId = places.ToDictionary(place => place.Id);
        var check = await CheckStopsAsync(row.StartDate, row.Stops, [], places, ct);
        var warnings = check.Warnings.Select(warning => warning.Message).ToList();
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
