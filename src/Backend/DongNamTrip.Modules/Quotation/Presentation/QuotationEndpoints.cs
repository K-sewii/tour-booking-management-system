using System.Security.Claims;
using DongNamTrip.Contracts;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Quotation;

public static class QuotationEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var itineraries = endpoints.MapGroup("/api/itineraries").RequireAuthorization().WithTags("Quotation");
        itineraries.MapPost("/{id:int}/requests", async (int id, PrivateTourRequestDraft draft, ClaimsPrincipal user,
            QuotationService service, CancellationToken ct) => ToHttp(await service.SubmitAsync(id, AccountId(user), draft, ct)))
            .WithValidation<PrivateTourRequestDraft>().WithSummary("Request a private tour from my itinerary");

        var requests = endpoints.MapGroup("/api/private-tour-requests").RequireAuthorization().WithTags("Quotation");
        requests.MapGet("", (ClaimsPrincipal user, QuotationService service, CancellationToken ct) =>
            service.ListAsync(AccountId(user), false, ct)).WithSummary("List my private-tour requests");
        requests.MapGet("/{id:int}", async (int id, ClaimsPrincipal user, QuotationService service, CancellationToken ct) =>
            await service.GetAsync(id, AccountId(user), false, ct) is { } request ? Results.Ok(request) : Results.NotFound())
            .WithSummary("Get my private-tour request");
        requests.MapPost("/{id:int}/quotes/{quoteId:int}/accept", async (int id, int quoteId, ClaimsPrincipal user,
            QuotationService service, CancellationToken ct) => ToHttp(await service.AcceptAsync(id, quoteId, AccountId(user), ct)))
            .WithSummary("Accept a valid quote and create a pending order");

        var orders = endpoints.MapGroup("/api/private-tour-orders").RequireAuthorization().WithTags("Quotation");
        orders.MapGet("", (ClaimsPrincipal user, QuotationService service, CancellationToken ct) =>
            service.OrdersAsync(AccountId(user), ct)).WithSummary("List my private-tour orders");

        var staff = endpoints.MapGroup("/api/staff/private-tour-requests")
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Operator)).WithTags("Quotation");
        staff.MapGet("", (QuotationService service, CancellationToken ct) => service.ListAsync(0, true, ct))
            .WithSummary("List private-tour requests for operations staff");
        staff.MapPost("/{id:int}/verifications", async (int id, ServiceVerificationDraft draft,
            QuotationService service, CancellationToken ct) => ToHttp(await service.VerifyAsync(id, draft, ct)))
            .WithValidation<ServiceVerificationDraft>().WithSummary("Record service availability");
        staff.MapPost("/{id:int}/quotes", async (int id, QuoteDraft draft, QuotationService service, CancellationToken ct) =>
            ToHttp(await service.IssueAsync(id, draft, ct)))
            .WithValidation<QuoteDraft>().WithSummary("Issue or replace a private-tour quote");
    }
}
