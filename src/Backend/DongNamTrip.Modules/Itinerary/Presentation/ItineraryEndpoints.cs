using System.Security.Claims;
using DongNamTrip.Contracts;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Itinerary;

/// <summary>Lớp giao diện (UI) của module Itinerary: ánh xạ HTTP endpoint. Không chứa quy tắc nghiệp vụ.</summary>
public static class ItineraryEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var itineraries = endpoints.MapGroup("/api/itineraries").WithTags("Itinerary");
        itineraries.MapGet("/preview", (int[]? ids, ItineraryService service, CancellationToken ct) => service.PreviewAsync(ids ?? [], ct))
            .WithSummary("Preview selected destinations").Produces<ItineraryPreview>();

        var mine = itineraries.MapGroup("").RequireAuthorization();
        mine.MapGet("", (ClaimsPrincipal user, ItineraryService service, CancellationToken ct) => service.ListAsync(AccountId(user), ct))
            .WithSummary("List my itineraries").Produces<List<Contracts.Itinerary>>().Produces(StatusCodes.Status401Unauthorized);
        mine.MapPost("", async (ItineraryDraft draft, ClaimsPrincipal user, ItineraryService service, CancellationToken ct) =>
            await service.SaveAsync(AccountId(user), draft, ct) is { } item ? Results.Ok(item) : Results.BadRequest())
            .WithValidation<ItineraryDraft>().WithSummary("Save a personal itinerary").WithDescription("The departure date cannot be in the past; destinations must be distinct and published.").Produces<Contracts.Itinerary>().Produces(StatusCodes.Status400BadRequest).Produces(StatusCodes.Status401Unauthorized);
    }
}
