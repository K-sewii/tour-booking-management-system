using DongNamTrip.Contracts;
using DongNamTrip.Modules.Catalog;

namespace DongNamTrip.Modules.Itinerary;

public sealed class ItineraryService(ItineraryRepository repository, ICatalogApi catalog)
{
    public async Task<ItineraryPreview> PreviewAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        var places = ids.Count == 0 ? [] : await catalog.DestinationsAsync([.. ids.Distinct().Take(Limits.MaxPlaces)], ct);
        return new(places, places.Sum(place => place.VisitMinutes));
    }

    public async Task<List<Contracts.Itinerary>> ListAsync(int accountId, CancellationToken ct)
    {
        var trips = await repository.ListAsync(accountId, ct);
        var places = (await catalog.DestinationsAsync([.. trips.SelectMany(trip => trip.DestinationIds).Distinct()], ct)).ToDictionary(place => place.Id);
        return trips.Select(trip => Build(trip.Id, accountId, trip.Name, trip.StartDate, trip.Guests, trip.DestinationIds, places)).ToList();
    }

    /// <returns>Null nếu ngày đi đã qua, địa điểm trùng lặp hoặc có địa điểm không còn hiển thị.</returns>
    public async Task<Contracts.Itinerary?> SaveAsync(int accountId, ItineraryDraft draft, CancellationToken ct)
    {
        if (draft.StartDate < DateOnly.FromDateTime(DateTime.Today) || draft.DestinationIds.Distinct().Count() != draft.DestinationIds.Length)
            return null;
        var places = await catalog.DestinationsAsync(draft.DestinationIds, ct);
        if (places.Count != draft.DestinationIds.Length) return null;
        var id = await repository.AddAsync(accountId, draft, ct);
        return Build(id, accountId, draft.Name.Trim(), draft.StartDate, draft.Guests, draft.DestinationIds, places.ToDictionary(place => place.Id));
    }

    // Địa điểm không còn hiển thị bị bỏ khỏi tên và tổng thời lượng, id gốc vẫn giữ.
    private static Contracts.Itinerary Build(int id, int accountId, string name, DateOnly startDate, int guests, int[] ids, Dictionary<int, Destination> places)
    {
        var known = ids.Where(places.ContainsKey).Select(placeId => places[placeId]).ToList();
        return new(id, accountId, name, startDate, guests, ids, known.Select(place => place.Name).ToArray(), known.Sum(place => place.VisitMinutes));
    }
}
