using DongNamTrip.Contracts;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Itinerary;

/// <summary>Lớp giao diện (UI) của module Itinerary: ánh xạ HTTP endpoint.</summary>
public static class ItineraryEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/itineraries/preview", (int[]? ids, ItineraryService service, CancellationToken ct) => service.PreviewAsync(ids ?? [], ct));
        endpoints.MapGet("/api/itineraries", (System.Security.Claims.ClaimsPrincipal user, ItineraryService service, CancellationToken ct) =>
            service.ListAsync(AccountId(user), ct)).RequireAuthorization();
        endpoints.MapPost("/api/itineraries", async (ItineraryDraft draft, System.Security.Claims.ClaimsPrincipal user, ItineraryService service, CancellationToken ct) =>
        {
            if (!Valid(draft) || draft.StartDate < DateOnly.FromDateTime(DateTime.Today) || draft.DestinationIds.Distinct().Count() != draft.DestinationIds.Length)
                return Results.BadRequest();

            var item = await service.SaveAsync(AccountId(user), draft, ct);
            return item is null ? Results.BadRequest() : Results.Ok(item);
        }).RequireAuthorization();
    }
}
