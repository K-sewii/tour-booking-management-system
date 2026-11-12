namespace DongNamTrip.Modules.Itinerary;

public sealed class ItineraryModule : IModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(new ScheduleChecker(configuration.GetValue("Itinerary:BufferMinutes", 30)));
        services.AddScoped<ItineraryRepository>();
        services.AddScoped<ItineraryVersionService>();
        services.AddScoped<ItineraryService>();
        services.AddScoped<IItineraryApi>(provider => provider.GetRequiredService<ItineraryService>());
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => ItineraryEndpoints.Map(endpoints);
}
