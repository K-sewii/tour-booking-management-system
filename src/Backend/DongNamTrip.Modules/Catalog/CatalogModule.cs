using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DongNamTrip.Modules.Catalog;

public sealed class CatalogModule : IModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<CatalogRepository>();
        services.AddScoped<TourAdminRepository>();
        services.AddScoped<TourAdminService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<ICatalogApi>(sp => sp.GetRequiredService<CatalogService>());
        services.AddScoped<PlaceAdminRepository>();
        services.AddScoped<PlaceAdminService>();
        services.AddScoped<ActivityRepository>();
        services.AddScoped<ActivityService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        CatalogEndpoints.Map(endpoints);
        PlaceAdminEndpoints.Map(endpoints);
        ActivityEndpoints.Map(endpoints);
        TourAdminEndpoints.Map(endpoints);
    }
}
