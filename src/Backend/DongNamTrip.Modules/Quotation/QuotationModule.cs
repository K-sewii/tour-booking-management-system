using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DongNamTrip.Modules.Quotation;

public sealed class QuotationModule : IModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<QuotationRepository>();
        services.AddScoped<QuoteRequestService>();
        services.AddScoped<QuotationAdminService>();
        services.AddHostedService<QuoteExpiryWorker>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        QuoteRequestEndpoints.Map(endpoints);
        QuotationAdminEndpoints.Map(endpoints);
    }
}
