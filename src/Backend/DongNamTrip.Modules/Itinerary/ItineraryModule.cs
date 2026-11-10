namespace DongNamTrip.Modules.Itinerary;

public sealed class ItineraryModule : IModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ItineraryRepository>();
        services.AddScoped<ItineraryService>();
        services.AddScoped<IItineraryApi>(provider => provider.GetRequiredService<ItineraryService>());
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => ItineraryEndpoints.Map(endpoints);
}
