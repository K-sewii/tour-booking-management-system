using DongNamTrip.Contracts;
using static DongNamTrip.Api.Modules.ModuleHttp;

namespace DongNamTrip.Api.Modules.Catalog;

public sealed class CatalogModule : IModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<CatalogStore>();
        services.AddSingleton<ICatalogApi>(sp => sp.GetRequiredService<CatalogStore>());
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
endpoints.MapGet("/api/destinations", (CatalogStore store) => store.Destinations());
endpoints.MapGet("/api/tours", (CatalogStore store) => store.Tours());
endpoints.MapPost("/api/destinations", (DestinationDraft draft, CatalogStore store) =>
    ValidDestination(draft) ? Results.Ok(store.Save(null, draft)) : Results.BadRequest()).RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Editor));
endpoints.MapPut("/api/destinations/{id:int}", (int id, DestinationDraft draft, CatalogStore store) =>
    !ValidDestination(draft) ? Results.BadRequest() : store.Save(id, draft) is { } result ? Results.Ok(result) : Results.NotFound()).RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Editor));
    }

    private static bool ValidDestination(DestinationDraft draft) => Valid(draft) && CatalogOptions.Regions.Contains(draft.Region) && CatalogOptions.Categories.Contains(draft.Category);
}
