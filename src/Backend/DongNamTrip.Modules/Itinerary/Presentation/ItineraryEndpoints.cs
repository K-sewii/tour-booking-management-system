using System.Security.Claims;
using DongNamTrip.Contracts;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Itinerary;

/// <summary>Lớp giao diện (UI) của module Itinerary: ánh xạ HTTP endpoint. Không chứa quy tắc nghiệp vụ.</summary>
public static class ItineraryEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var itineraries = endpoints.MapGroup("/api/itineraries");
        itineraries.MapGet("/preview", (int[]? ids, ItineraryService service, CancellationToken ct) => service.PreviewAsync(ids ?? [], ct));

        var mine = itineraries.MapGroup("").RequireAuthorization();
        mine.MapGet("", (ClaimsPrincipal user, ItineraryService service, CancellationToken ct) => service.ListAsync(AccountId(user), ct));
        mine.MapPost("", async (ItineraryDraft draft, ClaimsPrincipal user, ItineraryService service, CancellationToken ct) =>
            await service.SaveAsync(AccountId(user), draft, ct) is { } item ? Results.Ok(item) : Results.BadRequest())
            .WithValidation<ItineraryDraft>();
    }
}
