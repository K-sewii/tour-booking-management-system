using System.Security.Claims;
using DongNamTrip.Contracts;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Quotation;

/// <summary>Khách gửi yêu cầu tour riêng và xử lý báo giá (UC09, UC10, SCR10). Không chứa quy tắc nghiệp vụ.</summary>
public static class QuoteRequestEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var account = endpoints.MapGroup("/api/account").RequireAuthorization().WithTags("Quotation");

        account.MapPost("/quote-requests", async (QuoteRequestForm form, ClaimsPrincipal user, QuoteRequestService service, CancellationToken ct) =>
                ToHttpFresh(await service.SubmitAsync(AccountId(user), form, ct), item => $"/api/account/quote-requests/{item.Id}"))
            .WithSummary("Send a private-tour request from one of my itineraries")
            .Produces<QuoteRequestInfo>(StatusCodes.Status201Created).Produces<QuoteRequestInfo>();
        account.MapGet("/quote-requests", (ClaimsPrincipal user, QuoteRequestService service, CancellationToken ct) => service.ListMineAsync(AccountId(user), ct))
            .WithSummary("List my private-tour requests").Produces<List<QuoteRequestInfo>>();
        account.MapGet("/quote-requests/{id:int}", async (int id, ClaimsPrincipal user, QuoteRequestService service, CancellationToken ct) =>
                await service.GetMineAsync(AccountId(user), id, ct) is { } item ? Results.Ok(item) : Results.NotFound())
            .WithSummary("Get one of my private-tour requests").Produces<QuoteRequestDetail>().Produces(StatusCodes.Status404NotFound);
        account.MapPost("/quote-requests/{id:int}/cancel", async (int id, ClaimsPrincipal user, QuoteRequestService service, CancellationToken ct) =>
                ToHttp(await service.CancelAsync(AccountId(user), id, ct)))
            .WithSummary("Cancel one of my private-tour requests").Produces<QuoteRequestInfo>();
        account.MapGet("/quotes/{id:int}/program", async (int id, ClaimsPrincipal user, QuoteRequestService service, CancellationToken ct) =>
                await service.ProgramAsync(AccountId(user), id, ct) is { } stops ? Results.Ok(stops) : Results.NotFound())
            .WithSummary("Get the itinerary stops of one of my quotes").Produces<List<ProgramStop>>().Produces(StatusCodes.Status404NotFound);
        account.MapPost("/quotes/{id:int}/accept", async (int id, ClaimsPrincipal user, QuoteRequestService service, CancellationToken ct) =>
                ToHttpFresh(await service.AcceptQuoteAsync(AccountId(user), id, ct)))
            .WithSummary("Accept a valid quote and create a pending order").Produces<QuoteAccepted>(StatusCodes.Status201Created).Produces<QuoteAccepted>();
        account.MapPost("/quotes/{id:int}/change-request", async (int id, ChangeRequest request, ClaimsPrincipal user, QuoteRequestService service, CancellationToken ct) =>
                ToHttp(await service.AskChangeAsync(AccountId(user), id, request.Note, ct)))
            .WithSummary("Ask staff to change a quote").Produces<QuoteRequestInfo>();
    }
}
