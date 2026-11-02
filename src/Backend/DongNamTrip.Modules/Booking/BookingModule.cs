namespace DongNamTrip.Modules.Booking;

public sealed class BookingModule : IModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<BookingRepository>();
        services.AddScoped<PaymentService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => BookingEndpoints.Map(endpoints);
}
