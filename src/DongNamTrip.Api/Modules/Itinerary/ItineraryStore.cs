using DongNamTrip.Api.Modules.Catalog;
using DongNamTrip.Contracts;

namespace DongNamTrip.Api.Modules.Itinerary;

// Lưu trong bộ nhớ cho đến khi nối bảng LICHTRINH. Không dùng cho production.
public sealed class ItineraryStore(ICatalogApi catalog)
{
    private readonly object gate = new();
    private readonly Dictionary<string, List<Contracts.Itinerary>> itinerariesByOwner = new(StringComparer.Ordinal);

    public List<Contracts.Itinerary> Itineraries(string ownerKey)
    {
        lock (gate)
            return itinerariesByOwner.TryGetValue(ownerKey, out var itineraries)
                ? itineraries.Select(x => x with { DestinationIds = [..x.DestinationIds] }).ToList()
                : [];
    }

    public Contracts.Itinerary? SaveItinerary(string ownerKey, int userId, ItineraryDraft d)
    {
        lock (gate)
        {
            if (d.DestinationIds.Any(id => catalog.Destinations().All(x => x.Id != id)))
                return null;
            if (!itinerariesByOwner.TryGetValue(ownerKey, out var itineraries))
            {
                itineraries = [];
                itinerariesByOwner[ownerKey] = itineraries;
            }

            var item = new Contracts.Itinerary(itineraries.Count + 1, userId, d.Name.Trim(), d.StartDate, d.Guests, [..d.DestinationIds]);
            itineraries.Add(item);
            return item;
        }
    }

}
