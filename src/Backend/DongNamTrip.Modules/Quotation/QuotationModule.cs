namespace DongNamTrip.Modules.Quotation;

public sealed class QuotationModule : IModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<QuotationRepository>();
        services.AddScoped<QuotationService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => QuotationEndpoints.Map(endpoints);
}
