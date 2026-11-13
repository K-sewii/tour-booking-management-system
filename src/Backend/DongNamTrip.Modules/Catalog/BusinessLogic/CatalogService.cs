using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Catalog;

public sealed class CatalogService(CatalogRepository repository, TimeProvider time) : ICatalogApi
{
    public ClientRules Rules() => new(
        CatalogOptions.Regions.Select(region => new RegionOption(region, CatalogOptions.IllustrationFor(region))).ToArray(), CatalogOptions.Categories,
        Limits.TourPageSize, Limits.DestinationPageSize, Limits.PlannerListSize,
        Limits.ItineraryNameMax, Limits.GuestsMin, Limits.GuestsMax, Limits.MaxPlaces,
        Limits.NameMin, Limits.NameMax, Limits.PasswordMin, Limits.PasswordMax, Limits.PhoneMax,
        PaymentMethods.All);

    private static (int Page, int Size) Paging(int? page, int? size, int defaultSize) =>
        (Math.Clamp(page ?? 1, 1, Limits.MaxPage), Math.Clamp(size ?? defaultSize, 1, Limits.MaxPageSize));

    // Không ids: toàn bộ danh sách (Admin, MVC cũ); có ids: đúng thứ tự yêu cầu.
    public Task<List<Destination>> DestinationsAsync(IReadOnlyCollection<int>? ids, CancellationToken ct) => repository.DestinationsAsync(ids, ct);
    Task<List<Destination>> ICatalogApi.DestinationsAsync(IReadOnlyCollection<int> ids, CancellationToken ct) => repository.DestinationsAsync(ids, ct);
    public Task<PlannerCatalog> PlannerCatalogAsync(CancellationToken ct) => repository.PlannerCatalogAsync(ct);
    public Task<ItineraryStopDraft[]?> TourTemplateAsync(int tourId, CancellationToken ct) => repository.TourTemplateAsync(tourId, ct);
    public Task<List<PlannerSlot>> SlotsAsync(IReadOnlyCollection<int> ids, CancellationToken ct) => repository.SlotsAsync(ids, ct);
    public Task<DestinationDetail?> DestinationDetailAsync(int id, CancellationToken ct) =>
        repository.DestinationDetailAsync(id, DateOnly.FromDateTime(DateTime.Today), ct);
    public Task<List<Tour>> ToursAsync(CancellationToken ct) => repository.ToursAsync(ct);
    /// <summary>Tạo mới (<paramref name="id"/> null) hoặc cập nhật; vùng và loại hình phải thuộc <see cref="CatalogOptions"/>.</summary>
    public async Task<DestinationSaveResult> SaveDestinationAsync(int? id, DestinationDraft draft, CancellationToken ct)
    {
        if (!CatalogOptions.Regions.Contains(draft.Region) || !CatalogOptions.Categories.Contains(draft.Category))
            return DestinationSaveResult.Invalid;
        return await repository.SaveDestinationAsync(id, draft, ct) is { } saved ? DestinationSaveResult.Saved(saved) : DestinationSaveResult.NotFound;
    }

    public async Task<PagedResult<Destination>> SearchDestinationsAsync(string? q, string? region, string? category, int? page, int? size, CancellationToken ct)
    {
        var (number, take) = Paging(page, size, Limits.DestinationPageSize);
        var (items, total) = await repository.SearchDestinationsAsync(q?.Trim(), region, category, (number - 1) * take, take, ct);
        return new(items, total, number, take);
    }

    public async Task<PagedResult<Tour>> SearchToursAsync(string? q, string? region, string? category, string? sort, int? page, int? size, CancellationToken ct)
    {
        var (number, take) = Paging(page, size, Limits.TourPageSize);
        var (items, total) = await repository.SearchToursAsync(q?.Trim(), region, category, sort, (number - 1) * take, take, ct);
        return new(items, total, number, take);
    }

    public async Task<TourDetail?> TourDetailAsync(int id, CancellationToken ct)
    {
        var tour = await repository.TourAsync(id, ct);
        return tour is null ? null : new(tour, await repository.DestinationsAsync(tour.DestinationIds, ct));
    }

    public async Task<TourEstimate?> EstimateAsync(int id, int guests, CancellationToken ct)
    {
        if (guests is < Limits.GuestsMin or > Limits.GuestsMax) return null;
        var tour = await repository.TourAsync(id, ct);
        return tour is null ? null : new(guests, tour.ReferencePrice * guests);
    }

    public async Task<bool> ExistsAsync(string kind, int id, CancellationToken ct) => kind switch
    {
        ItemKinds.Tour => await repository.TourAsync(id, ct) is not null,
        ItemKinds.Destination => (await repository.DestinationsAsync([id], ct)).Count == 1,
        _ => false
    };

    public Task<bool> PlaceExistsAsync(int id, CancellationToken ct) => repository.PlaceExistsAsync(id, ct);

    private DateTime Now => time.GetLocalNow().DateTime;

    /// <summary>Lịch Open của tour Published, chưa khởi hành; null nếu tour không tồn tại hoặc chưa công bố.</summary>
    public async Task<List<DepartureInfo>?> DeparturesAsync(int tourId, CancellationToken ct) =>
        await repository.TourVisibleAsync(tourId, ct) ? await repository.DeparturesAsync(tourId, Now, ct) : null;

    public Task<List<SlotDetail>> SlotDetailsAsync(IReadOnlyCollection<int> slotIds, CancellationToken ct) =>
        slotIds.Count == 0 ? Task.FromResult(new List<SlotDetail>()) : repository.SlotDetailsAsync(slotIds, ct);

    public Task<Dictionary<int, DestinationHours>> HoursAsync(IReadOnlyCollection<int> destinationIds, CancellationToken ct) =>
        destinationIds.Count == 0 ? Task.FromResult(new Dictionary<int, DestinationHours>()) : repository.HoursAsync(destinationIds, ct);

    public Task<List<ProgramStop>> ProgramAsync(int tourId, CancellationToken ct) => repository.TourProgramAsync(tourId, ct);
    public Task<Dictionary<int, string>> PlaceNamesAsync(IReadOnlyCollection<int> ids, CancellationToken ct) => repository.PlaceNamesAsync(ids, ct);

    public async Task<DepartureInfo?> DepartureAsync(int id, CancellationToken ct) =>
        (await repository.DeparturesAsync([id], ct)).GetValueOrDefault(id);

    public Task<Dictionary<int, DepartureInfo>> DeparturesAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        ids.Count == 0 ? Task.FromResult(new Dictionary<int, DepartureInfo>()) : repository.DeparturesAsync(ids, ct);

    public async Task<HoldResult> HoldSeatsAsync(int departureId, int guests, CancellationToken ct)
    {
        if (guests > 0 && await repository.TryHoldAsync(departureId, guests, Now, ct) == 1)
            return new HoldResult(SeatHold.Held, (await repository.StateAsync(departureId, ct))!.Remaining);
        var state = await repository.StateAsync(departureId, ct);
        if (state is null) return new HoldResult(SeatHold.NotFound, 0);
        if (state.Capacity - state.Held < guests) return new HoldResult(SeatHold.Full, state.Remaining);
        return new HoldResult(SeatHold.Closed, state.Remaining);
    }

    public Task ReleaseSeatsAsync(int departureId, int guests, CancellationToken ct) =>
        guests <= 0 ? Task.CompletedTask : repository.ReleaseAsync(departureId, guests, ct);

    public async Task<Dictionary<int, string>> NamesAsync(string kind, IReadOnlyCollection<int> ids, CancellationToken ct) => kind switch
    {
        ItemKinds.Tour => ids.Count == 0 ? [] : await repository.TourNamesAsync(ids, ct),
        ItemKinds.Destination => ids.Count == 0 ? [] : (await repository.DestinationsAsync(ids, ct)).ToDictionary(item => item.Id, item => item.Name),
        _ => []
    };
}

public sealed record DestinationSaveResult(Destination? Item, DestinationSaveStatus Status)
{
    public static DestinationSaveResult Saved(Destination item) => new(item, DestinationSaveStatus.Saved);
    public static DestinationSaveResult Invalid { get; } = new(null, DestinationSaveStatus.Invalid);
    public static DestinationSaveResult NotFound { get; } = new(null, DestinationSaveStatus.NotFound);
}

public enum DestinationSaveStatus { Saved, Invalid, NotFound }
