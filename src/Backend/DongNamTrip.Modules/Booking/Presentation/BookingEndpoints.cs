using System.Security.Claims;
using DongNamTrip.Contracts;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Booking;

/// <summary>Lớp giao diện (UI) của module Booking: ánh xạ HTTP endpoint. Không truy cập dữ liệu, không chứa quy tắc nghiệp vụ.</summary>
public static class BookingEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var account = endpoints.MapGroup("/api/account").RequireAuthorization();

        account.MapGet("/payments", async (ClaimsPrincipal user, PaymentService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(AccountId(user), ct)));

        account.MapGet("/payments/{id:int}", async (int id, ClaimsPrincipal user, PaymentService service, CancellationToken ct) =>
            await service.GetAsync(AccountId(user), id, ct) is { } entry ? Results.Ok(entry) : Results.NotFound());

        account.MapGet("/payment-choice", async (ClaimsPrincipal user, PaymentService service, CancellationToken ct) =>
            Results.Ok(await service.ChoiceAsync(AccountId(user), ct)));

        account.MapPut("/payment-choice", async (PaymentChoiceRequest input, ClaimsPrincipal user, PaymentService service, CancellationToken ct) =>
            await service.SaveChoiceAsync(AccountId(user), input.MethodCode, ct) is { } choice ? Results.Ok(choice) : Results.BadRequest());
    }
}
