using System.Security.Claims;
using DongNamTrip.Contracts;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Catalog;

/// <summary>Quản trị tour và lịch khởi hành (UC16, SCR16). Không chứa quy tắc nghiệp vụ.</summary>
public static class TourAdminEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin")
            .WithTags("Catalog")
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Operator));

        var tours = admin.MapGroup("/tours");
        tours.MapGet("", (string? status, TourAdminService service, CancellationToken ct) => service.ListAsync(status, ct))
            .WithSummary("List tours for staff").Produces<List<AdminTour>>();
        tours.MapGet("/{id:int}", async (int id, TourAdminService service, CancellationToken ct) =>
                await service.GetAsync(id, ct) is { } item ? Results.Ok(item) : Results.NotFound())
            .WithSummary("Get a tour for staff").Produces<AdminTourDetail>().Produces(StatusCodes.Status404NotFound);
        tours.MapPost("", async (TourForm form, ClaimsPrincipal user, TourAdminService service, CancellationToken ct) =>
                ToHttp(await service.SaveAsync(null, form, AccountId(user), ct), item => Results.Created($"/api/admin/tours/{item.Id}", item)))
            .WithSummary("Create a draft tour").Produces<AdminTour>(StatusCodes.Status201Created);
        tours.MapPut("/{id:int}", async (int id, TourForm form, ClaimsPrincipal user, TourAdminService service, CancellationToken ct) =>
                ToHttp(await service.SaveAsync(id, form, AccountId(user), ct)))
            .WithSummary("Update a tour").Produces<AdminTour>();
        tours.MapPut("/{id:int}/places", async (int id, List<TourStopForm> stops, ClaimsPrincipal user, TourAdminService service, CancellationToken ct) =>
                ToHttp(await service.SetPlacesAsync(id, stops, AccountId(user), ct)))
            .WithSummary("Replace the stops of a tour").Produces<AdminTourDetail>();
        tours.MapPost("/{id:int}/status", async (int id, StatusRequest request, ClaimsPrincipal user, TourAdminService service, CancellationToken ct) =>
                ToHttp(await service.SetStatusAsync(id, request.Status, request.Reason, AccountId(user), ct)))
            .WithSummary("Publish or deactivate a tour").Produces<AdminTour>();
        tours.MapGet("/{id:int}/departures", async (int id, TourAdminService service, CancellationToken ct) =>
                ToHttp(await service.DeparturesAsync(id, ct)))
            .WithSummary("List departures of a tour").Produces<List<AdminDeparture>>();
        tours.MapPost("/{id:int}/departures", async (int id, DepartureForm form, ClaimsPrincipal user, TourAdminService service, CancellationToken ct) =>
                ToHttp(await service.SaveDepartureAsync(id, null, form, AccountId(user), ct), item => Results.Created($"/api/admin/departures/{item.Id}", item)))
            .WithSummary("Create an open departure").Produces<AdminDeparture>(StatusCodes.Status201Created);

        var departures = admin.MapGroup("/departures");
        departures.MapPut("/{id:int}", async (int id, DepartureForm form, ClaimsPrincipal user, TourAdminService service, CancellationToken ct) =>
                ToHttp(await service.SaveDepartureAsync(0, id, form, AccountId(user), ct)))
            .WithSummary("Update a departure").Produces<AdminDeparture>();
        departures.MapPost("/{id:int}/deactivate", async (int id, ClaimsPrincipal user, TourAdminService service, CancellationToken ct) =>
                ToHttp(await service.DeactivateDepartureAsync(id, AccountId(user), ct)))
            .WithSummary("Deactivate a departure").Produces<AdminDeparture>();
    }
}
