namespace DongNamTrip.Modules.Narration;

public sealed class NarrationModule : IModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<AudioFileStore>();
        services.AddScoped<NarrationRepository>();
        services.AddScoped<NarrationService>();
        services.AddScoped<ListenRepository>();
        services.AddScoped<ListenService>();
        services.AddScoped<INarrationApi>(sp => sp.GetRequiredService<ListenService>());
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => NarrationEndpoints.Map(endpoints);
}
