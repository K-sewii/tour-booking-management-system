using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Catalog;

/// <summary>Lớp giao diện (UI) của module Catalog: ánh xạ HTTP endpoint. Không chứa quy tắc nghiệp vụ.</summary>
public static class CatalogEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api").WithTags("Catalog");

        api.MapGet("/meta", (CatalogService catalog) => catalog.Rules())
            .WithSummary("Get client rules and catalog options")
            .Produces<ClientRules>();

        MapDestinations(api.MapGroup("/destinations"));
        MapTours(api.MapGroup("/tours"));
    }

    private static void MapDestinations(RouteGroupBuilder destinations)
    {
        // Danh sách phẳng: dành cho Admin và bản MVC cũ.
        destinations.MapGet("", (
            int[]? ids,
            CatalogService catalog,
            CancellationToken ct) =>
            catalog.DestinationsAsync(ids, ct))
            .WithSummary("List published destinations")
            .Produces<List<Destination>>();

        destinations.MapGet("/search", (
            string? q,
            string? region,
            string? category,
            int? page,
            int? pageSize,
            CatalogService catalog,
            CancellationToken ct) =>
            catalog.SearchDestinationsAsync(
                q, region, category, page, pageSize, ct))
            .WithSummary("Search published destinations")
            .Produces<PagedResult<Destination>>();

        destinations.MapGet("/{id:int}", async (
            int id,
            CatalogService catalog,
            CancellationToken ct) =>
            await catalog.DestinationDetailAsync(id, ct) is { } item
                ? Results.Ok(item)
                : Results.NotFound())
            .WithSummary("Get a published destination")
            .Produces<DestinationDetail>()
            .Produces(StatusCodes.Status404NotFound);

        var editors = destinations.MapGroup("")
            .RequireAuthorization(policy =>
                policy.RequireRole(AppRoles.Admin, AppRoles.Editor));

        editors.MapPost("", async (
            DestinationDraft draft,
            CatalogService catalog,
            CancellationToken ct) =>
            ToResult(await catalog.SaveDestinationAsync(null, draft, ct)))
            .WithValidation<DestinationDraft>()
            .WithSummary("Create a destination")
            .Produces<Destination>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        editors.MapPut("/{id:int}", async (
            int id,
            DestinationDraft draft,
            CatalogService catalog,
            CancellationToken ct) =>
            ToResult(await catalog.SaveDestinationAsync(id, draft, ct)))
            .WithValidation<DestinationDraft>()
            .WithSummary("Update a destination")
            .Produces<Destination>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static void MapTours(RouteGroupBuilder tours)
    {
        tours.MapGet("", (
            CatalogService catalog,
            CancellationToken ct) =>
            catalog.ToursAsync(ct))
            .WithSummary("List published tours")
            .Produces<List<Tour>>();

        tours.MapGet("/search", (
            string? q,
            string? region,
            string? category,
            string? sort,
            int? page,
            int? pageSize,
            CatalogService catalog,
            CancellationToken ct) =>
            catalog.SearchToursAsync(
                q, region, category, sort, page, pageSize, ct))
            .WithSummary("Search published tours")
            .Produces<PagedResult<Tour>>();

        tours.MapGet("/{id:int}", async (
            int id,
            CatalogService catalog,
            CancellationToken ct) =>
            await catalog.TourDetailAsync(id, ct) is { } item
                ? Results.Ok(item)
                : Results.NotFound())
            .WithSummary("Get a published tour")
            .Produces<TourDetail>()
            .Produces(StatusCodes.Status404NotFound);

        tours.MapGet("/{id:int}/estimate", async (
            int id,
            int guests,
            CatalogService catalog,
            CancellationToken ct) =>
            await catalog.EstimateAsync(id, guests, ct) is { } item
                ? Results.Ok(item)
                : Results.BadRequest())
            .WithSummary("Estimate a tour reference price")
            .WithDescription(
                "Uses the reference price only; this does not create a booking or payment.")
            .Produces<TourEstimate>()
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static IResult ToResult(DestinationSaveResult result) =>
        result.Status switch
        {
            DestinationSaveStatus.Saved => Results.Ok(result.Item),
            DestinationSaveStatus.NotFound => Results.NotFound(),
            _ => Results.BadRequest()
        };
}