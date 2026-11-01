using System.Security.Claims;
using DongNamTrip.Modules.Persistence;
using DongNamTrip.Contracts;
using Microsoft.EntityFrameworkCore;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Booking;

public sealed class BookingModule : IModule
{
    public void Register(IServiceCollection services, IConfiguration configuration) { }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => BookingEndpoints.Map(endpoints);
}
