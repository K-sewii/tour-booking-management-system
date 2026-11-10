using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Itinerary;

public interface IItineraryApi
{
    Task<Contracts.Itinerary?> GetAsync(int id, int accountId, CancellationToken ct);
}
