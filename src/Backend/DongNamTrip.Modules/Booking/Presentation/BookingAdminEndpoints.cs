using System.Security.Claims;
using DongNamTrip.Contracts;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Booking;

/// <summary>Quản lý đơn của nhân viên (UC13, UC19, UC20, SCR17). Không chứa quy tắc nghiệp vụ.</summary>
public static class BookingAdminEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/bookings").WithTags("Booking");
        var read = admin.MapGroup("").RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Operator, AppRoles.Finance));
        read.MapGet("", (string? status, DateTime? from, DateTime? to, string? q, bool? overdue, int? page, int? size, BookingAdminService service, CancellationToken ct) =>
                service.ListAsync(new BookingFilter(status, from, to, q, overdue, page, size), ct))
            .WithSummary("List orders for staff").Produces<PagedResult<AdminBooking>>();
        read.MapGet("/{id:int}", async (int id, BookingAdminService service, CancellationToken ct) =>
                await service.GetAsync(id, ct) is { } item ? Results.Ok(item) : Results.NotFound())
            .WithSummary("Get an order for staff").Produces<AdminBookingDetail>().Produces(StatusCodes.Status404NotFound);

        var handle = admin.MapGroup("").RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Operator));
        handle.MapPost("/{id:int}/confirm", async (int id, ClaimsPrincipal user, BookingAdminService service, CancellationToken ct) =>
                ToHttp(await service.ConfirmAsync(id, AccountId(user), ct)))
            .WithSummary("Confirm an order").Produces<AdminBooking>();
        handle.MapPost("/{id:int}/adjust", async (int id, AdjustForm form, ClaimsPrincipal user, BookingAdminService service, CancellationToken ct) =>
                ToHttp(await service.AdjustAsync(id, form, AccountId(user), ct)))
            .WithSummary("Adjust a confirmed order").Produces<AdminBookingDetail>();
        handle.MapPost("/{id:int}/cancel-decision", async (int id, CancelDecision decision, ClaimsPrincipal user, BookingAdminService service, CancellationToken ct) =>
                ToHttp(await service.DecideCancelAsync(id, decision, AccountId(user), ct)))
            .WithSummary("Approve or reject a cancel request").Produces<AdminBooking>();
        handle.MapPost("/{id:int}/complete", async (int id, ClaimsPrincipal user, BookingAdminService service, CancellationToken ct) =>
                ToHttp(await service.CompleteAsync(id, AccountId(user), ct)))
            .WithSummary("Complete an order after the trip date").Produces<AdminBooking>();

        admin.MapGroup("").RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Finance))
            .MapPost("/{id:int}/money", async (int id, MoneyForm form, ClaimsPrincipal user, BookingAdminService service, CancellationToken ct) =>
                ToHttpFresh(await service.RecordMoneyAsync(id, form, AccountId(user), ct)))
            .WithSummary("Record a receipt or refund").Produces<MoneyEntry>(StatusCodes.Status201Created).Produces<MoneyEntry>();
    }
}
