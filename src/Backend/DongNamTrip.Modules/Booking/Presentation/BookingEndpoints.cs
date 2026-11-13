using System.Security.Claims;
using DongNamTrip.Contracts;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Booking;

/// <summary>Lớp giao diện (UI) của module Booking: ánh xạ HTTP endpoint. Không truy cập dữ liệu, không chứa quy tắc nghiệp vụ.</summary>
public static class BookingEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var account = endpoints.MapGroup("/api/account").RequireAuthorization().WithTags("Booking");

        account.MapPost("/bookings", async (BookingForm form, ClaimsPrincipal user, BookingService service, CancellationToken ct) =>
            ToHttpFresh(await service.PlaceAsync(AccountId(user), form, ct), item => $"/api/account/bookings/{item.Id}"))
            .WithSummary("Place a booking on a departure and hold seats").Produces<BookingInfo>(StatusCodes.Status201Created).Produces<BookingInfo>().Produces(StatusCodes.Status409Conflict);

        account.MapGet("/bookings", (ClaimsPrincipal user, BookingService service, CancellationToken ct) => service.ListAsync(AccountId(user), ct))
            .WithSummary("List my bookings").Produces<List<BookingInfo>>();

        account.MapGet("/bookings/{id:int}", async (int id, ClaimsPrincipal user, BookingService service, CancellationToken ct) =>
            await service.GetAsync(AccountId(user), id, ct) is { } item ? Results.Ok(item) : Results.NotFound())
            .WithSummary("Get one of my bookings").Produces<BookingDetail>().Produces(StatusCodes.Status404NotFound);

        account.MapPost("/bookings/{id:int}/cancel", async (int id, CancelRequest request, ClaimsPrincipal user, BookingService service, CancellationToken ct) =>
            ToHttp(await service.RequestCancelAsync(AccountId(user), id, request.Reason, ct)))
            .WithSummary("Request to cancel one of my bookings").Produces<BookingInfo>();

        account.MapGet("/payments", async (ClaimsPrincipal user, PaymentService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(AccountId(user), ct))).WithSummary("List my recorded orders and transactions").WithDescription("Read-only history; creating an order, taking payment, refunds and gateway integration are not implemented.").Produces<List<PaymentEntry>>().Produces(StatusCodes.Status401Unauthorized);

        account.MapGet("/payments/{id:int}", async (int id, ClaimsPrincipal user, PaymentService service, CancellationToken ct) =>
            await service.GetAsync(AccountId(user), id, ct) is { } entry ? Results.Ok(entry) : Results.NotFound()).WithSummary("Get one of my recorded orders").Produces<PaymentEntry>().Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status404NotFound);

        account.MapGet("/payment-choice", async (ClaimsPrincipal user, PaymentService service, CancellationToken ct) =>
            Results.Ok(await service.ChoiceAsync(AccountId(user), ct))).WithSummary("Get my payment-method choice").Produces<PaymentChoiceDto>().Produces(StatusCodes.Status401Unauthorized);

        account.MapPut("/payment-choice", async (PaymentChoiceRequest input, ClaimsPrincipal user, PaymentService service, CancellationToken ct) =>
            await service.SaveChoiceAsync(AccountId(user), input.MethodCode, ct) is { } choice ? Results.Ok(choice) : Results.BadRequest()).WithSummary("Save my payment-method choice").WithDescription("Only `bank_transfer` and `cash` are accepted. This records a preference; it does not charge or reserve anything.").Produces<PaymentChoiceDto>().Produces(StatusCodes.Status400BadRequest).Produces(StatusCodes.Status401Unauthorized);
    }
}
