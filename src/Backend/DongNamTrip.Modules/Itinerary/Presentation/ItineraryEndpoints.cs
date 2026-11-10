using System.Security.Claims;
using DongNamTrip.Contracts;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Itinerary;

/// <summary>Lớp giao diện (UI) của module Itinerary: ánh xạ HTTP endpoint. Không chứa quy tắc nghiệp vụ.</summary>
public static class ItineraryEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var planner = endpoints.MapGroup("/api/planner").WithTags("Itinerary");
        planner.MapGet("/catalog", (ItineraryService service, CancellationToken ct) => service.CatalogAsync(ct))
            .WithSummary("List published planning places, activities and upcoming slots").Produces<PlannerCatalog>();
        planner.MapGet("/tours/{id:int}", async (int id, ItineraryService service, CancellationToken ct) =>
            await service.TourTemplateAsync(id, ct) is { } stops ? Results.Ok(stops) : Results.NotFound())
            .WithSummary("Copy a published tour as itinerary stops").Produces<ItineraryStopDraft[]>();
        var itineraries = endpoints.MapGroup("/api/itineraries").WithTags("Itinerary");
        itineraries.MapGet("/preview", (int[]? ids, ItineraryService service, CancellationToken ct) => service.PreviewAsync(ids ?? [], ct))
            .WithSummary("Preview selected destinations").Produces<ItineraryPreview>();

        var mine = itineraries.MapGroup("").RequireAuthorization();
        mine.MapGet("", (ClaimsPrincipal user, ItineraryService service, CancellationToken ct) => service.ListAsync(AccountId(user), ct))
            .WithSummary("List my itineraries").Produces<List<Contracts.Itinerary>>().Produces(StatusCodes.Status401Unauthorized);
        mine.MapGet("/{id:int}", async (int id, ClaimsPrincipal user, ItineraryService service, CancellationToken ct) =>
            await service.GetAsync(id, AccountId(user), ct) is { } item ? Results.Ok(item) : Results.NotFound())
            .WithSummary("Get one of my itineraries").Produces<Contracts.Itinerary>().Produces(StatusCodes.Status404NotFound);
        mine.MapPost("", async (ItineraryDraft draft, ClaimsPrincipal user, ItineraryService service, CancellationToken ct) =>
            ToHttp(await service.SaveAsync(null, AccountId(user), draft, ct)))
            .WithValidation<ItineraryDraft>().WithSummary("Save a personal itinerary").WithDescription("The departure date cannot be in the past; destinations must be distinct and published.").Produces<Contracts.Itinerary>().Produces(StatusCodes.Status400BadRequest).Produces(StatusCodes.Status401Unauthorized);
        mine.MapPut("/{id:int}", async (int id, ItineraryDraft draft, ClaimsPrincipal user, ItineraryService service, CancellationToken ct) =>
            ToHttp(await service.SaveAsync(id, AccountId(user), draft, ct)))
            .WithValidation<ItineraryDraft>().WithSummary("Update one of my itineraries").Produces<Contracts.Itinerary>().Produces(StatusCodes.Status404NotFound);
    }
}
