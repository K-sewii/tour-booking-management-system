namespace DongNamTrip.Modules.Catalog;

public sealed class CatalogModule : IModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<CatalogRepository>();
        services.AddScoped<CatalogService>();
        services.AddScoped<ICatalogApi>(sp => sp.GetRequiredService<CatalogService>());
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => CatalogEndpoints.Map(endpoints);
}
