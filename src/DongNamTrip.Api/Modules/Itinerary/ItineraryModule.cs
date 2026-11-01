using System.Security.Claims;
using DongNamTrip.Contracts;
using static DongNamTrip.Api.Modules.ModuleHttp;

namespace DongNamTrip.Api.Modules.Itinerary;

public sealed class ItineraryModule : IModule
{
    public void Register(IServiceCollection services, IConfiguration configuration) => services.AddSingleton<ItineraryStore>();

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
endpoints.MapGet("/api/itineraries", (ClaimsPrincipal user, ItineraryStore store) =>
    store.Itineraries(OwnerKey(user))).RequireAuthorization();
endpoints.MapPost("/api/itineraries", (ItineraryDraft draft, ClaimsPrincipal user, ItineraryStore store) =>
{
    if (!Valid(draft) || draft.StartDate < DateOnly.FromDateTime(DateTime.Today) || draft.DestinationIds.Distinct().Count() != draft.DestinationIds.Length)
        return Results.BadRequest();

    var item = store.SaveItinerary(OwnerKey(user), AccountId(user), draft);
    return item is null ? Results.BadRequest() : Results.Ok(item);
}).RequireAuthorization();
    }

    private static string OwnerKey(ClaimsPrincipal user) => $"account:{AccountId(user)}";
}
