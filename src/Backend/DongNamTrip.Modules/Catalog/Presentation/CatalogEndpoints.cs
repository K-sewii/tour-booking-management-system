using DongNamTrip.Contracts;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Catalog;

/// <summary>Lớp giao diện (UI) của module Catalog: ánh xạ HTTP endpoint.</summary>
public static class CatalogEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/meta", (CatalogService catalog) => catalog.Rules());

        // Danh sách phẳng: dành cho Admin và bản MVC cũ.
        endpoints.MapGet("/api/destinations", (int[]? ids, CatalogService catalog, CancellationToken ct) => catalog.DestinationsAsync(ids, ct));
        endpoints.MapGet("/api/tours", (CatalogService catalog, CancellationToken ct) => catalog.ToursAsync(ct));
        endpoints.MapGet("/api/admin/destinations", (CatalogService catalog, CancellationToken ct) => catalog.AdminDestinationsAsync(ct))
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Editor));

        endpoints.MapGet("/api/destinations/search", (string? q, string? region, string? category, int? page, int? pageSize, CatalogService catalog, CancellationToken ct) =>
            catalog.SearchDestinationsAsync(q, region, category, page, pageSize, ct));
        endpoints.MapGet("/api/destinations/{id:int}", async (int id, CatalogService catalog, CancellationToken ct) =>
            (await catalog.DestinationsAsync([id], ct)).FirstOrDefault() is { } item ? Results.Ok(item) : Results.NotFound());
        endpoints.MapGet("/api/tours/search", (string? q, string? region, string? category, string? sort, int? page, int? pageSize, CatalogService catalog, CancellationToken ct) =>
            catalog.SearchToursAsync(q, region, category, sort, page, pageSize, ct));
        endpoints.MapGet("/api/tours/{id:int}", async (int id, CatalogService catalog, CancellationToken ct) =>
            await catalog.TourDetailAsync(id, ct) is { } item ? Results.Ok(item) : Results.NotFound());
        endpoints.MapGet("/api/tours/{id:int}/estimate", async (int id, int guests, CatalogService catalog, CancellationToken ct) =>
            await catalog.EstimateAsync(id, guests, ct) is { } item ? Results.Ok(item) : Results.BadRequest());

        endpoints.MapPost("/api/destinations", async (DestinationDraft draft, CatalogService catalog, CancellationToken ct) =>
            ValidDestination(draft) ? Results.Ok(await catalog.SaveDestinationAsync(null, draft, ct)) : Results.BadRequest())
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Editor));
        endpoints.MapPut("/api/destinations/{id:int}", async (int id, DestinationDraft draft, CatalogService catalog, CancellationToken ct) =>
            !ValidDestination(draft) ? Results.BadRequest() : await catalog.SaveDestinationAsync(id, draft, ct) is { } result ? Results.Ok(result) : Results.NotFound())
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Editor));
    }

    private static bool ValidDestination(DestinationDraft draft) => Valid(draft) && CatalogOptions.Regions.Contains(draft.Region) && CatalogOptions.Categories.Contains(draft.Category);
}
