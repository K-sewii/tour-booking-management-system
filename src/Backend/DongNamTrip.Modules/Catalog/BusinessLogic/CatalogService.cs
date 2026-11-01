using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Catalog;

public sealed class CatalogService(CatalogRepository repository) : ICatalogApi
{
    public ClientRules Rules() => new(
        CatalogOptions.Regions.Select(region => new RegionOption(region, CatalogOptions.IllustrationFor(region))).ToArray(), CatalogOptions.Categories,
        Limits.TourPageSize, Limits.DestinationPageSize, Limits.PlannerListSize,
        Limits.ItineraryNameMax, Limits.GuestsMin, Limits.GuestsMax, Limits.MaxPlaces,
        Limits.NameMin, Limits.NameMax, Limits.PasswordMin, Limits.PasswordMax, Limits.PhoneMax);

    private static (int Page, int Size) Paging(int? page, int? size, int defaultSize) =>
        (Math.Clamp(page ?? 1, 1, Limits.MaxPage), Math.Clamp(size ?? defaultSize, 1, Limits.MaxPageSize));

    // Không ids: toàn bộ danh sách (Admin, MVC cũ); có ids: đúng thứ tự yêu cầu.
    public Task<List<Destination>> DestinationsAsync(IReadOnlyCollection<int>? ids, CancellationToken ct) => repository.DestinationsAsync(ids, ct);
    Task<List<Destination>> ICatalogApi.DestinationsAsync(IReadOnlyCollection<int> ids, CancellationToken ct) => repository.DestinationsAsync(ids, ct);
    public Task<List<Destination>> AdminDestinationsAsync(CancellationToken ct) => repository.AdminDestinationsAsync(ct);
    public Task<List<Tour>> ToursAsync(CancellationToken ct) => repository.ToursAsync(ct);
    public Task<Destination?> SaveDestinationAsync(int? id, DestinationDraft draft, CancellationToken ct) => repository.SaveDestinationAsync(id, draft, ct);

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
        "tour" => await repository.TourAsync(id, ct) is not null,
        "destination" => (await repository.DestinationsAsync([id], ct)).Count == 1,
        _ => false
    };

    public async Task<Dictionary<int, string>> NamesAsync(string kind, IReadOnlyCollection<int> ids, CancellationToken ct) => kind switch
    {
        "tour" => ids.Count == 0 ? [] : await repository.TourNamesAsync(ids, ct),
        "destination" => ids.Count == 0 ? [] : (await repository.DestinationsAsync(ids, ct)).ToDictionary(item => item.Id, item => item.Name),
        _ => []
    };
}
