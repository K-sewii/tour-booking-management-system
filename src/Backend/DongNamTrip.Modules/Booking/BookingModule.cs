using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DongNamTrip.Modules.Booking;

public sealed class BookingModule : IModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<BookingRepository>();
        services.AddScoped<PaymentService>();
        services.AddScoped<IBookingStatisticsApi, BookingStatisticsService>();
        services.AddScoped<BookingService>();
        services.AddScoped<IBookingApi>(sp => sp.GetRequiredService<BookingService>());
        services.AddScoped<BookingAdminService>();
        services.AddHostedService<HoldExpiryWorker>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        BookingEndpoints.Map(endpoints);
        BookingAdminEndpoints.Map(endpoints);
    }
}
