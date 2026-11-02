using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Catalog;

/// <summary>Lớp giao diện (UI) của module Catalog: ánh xạ HTTP endpoint. Không chứa quy tắc nghiệp vụ.</summary>
public static class CatalogEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api");
        api.MapGet("/meta", (CatalogService catalog) => catalog.Rules());
        api.MapGet("/admin/destinations", (CatalogService catalog, CancellationToken ct) => catalog.AdminDestinationsAsync(ct))
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Editor));

        MapDestinations(api.MapGroup("/destinations"));
        MapTours(api.MapGroup("/tours"));
    }

    private static void MapDestinations(RouteGroupBuilder destinations)
    {
        // Danh sách phẳng: dành cho Admin và bản MVC cũ.
        destinations.MapGet("", (int[]? ids, CatalogService catalog, CancellationToken ct) => catalog.DestinationsAsync(ids, ct));
        destinations.MapGet("/search", (string? q, string? region, string? category, int? page, int? pageSize, CatalogService catalog, CancellationToken ct) =>
            catalog.SearchDestinationsAsync(q, region, category, page, pageSize, ct));
        destinations.MapGet("/{id:int}", async (int id, CatalogService catalog, CancellationToken ct) =>
            (await catalog.DestinationsAsync([id], ct)).FirstOrDefault() is { } item ? Results.Ok(item) : Results.NotFound());

        var editors = destinations.MapGroup("").RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Editor));
        editors.MapPost("", async (DestinationDraft draft, CatalogService catalog, CancellationToken ct) =>
            ToResult(await catalog.SaveDestinationAsync(null, draft, ct))).WithValidation<DestinationDraft>();
        editors.MapPut("/{id:int}", async (int id, DestinationDraft draft, CatalogService catalog, CancellationToken ct) =>
            ToResult(await catalog.SaveDestinationAsync(id, draft, ct))).WithValidation<DestinationDraft>();
    }

    private static void MapTours(RouteGroupBuilder tours)
    {
        tours.MapGet("", (CatalogService catalog, CancellationToken ct) => catalog.ToursAsync(ct));
        tours.MapGet("/search", (string? q, string? region, string? category, string? sort, int? page, int? pageSize, CatalogService catalog, CancellationToken ct) =>
            catalog.SearchToursAsync(q, region, category, sort, page, pageSize, ct));
        tours.MapGet("/{id:int}", async (int id, CatalogService catalog, CancellationToken ct) =>
            await catalog.TourDetailAsync(id, ct) is { } item ? Results.Ok(item) : Results.NotFound());
        tours.MapGet("/{id:int}/estimate", async (int id, int guests, CatalogService catalog, CancellationToken ct) =>
            await catalog.EstimateAsync(id, guests, ct) is { } item ? Results.Ok(item) : Results.BadRequest());
    }

    private static IResult ToResult(DestinationSaveResult result) => result.Status switch
    {
        DestinationSaveStatus.Saved => Results.Ok(result.Item),
        DestinationSaveStatus.NotFound => Results.NotFound(),
        _ => Results.BadRequest()
    };
}
