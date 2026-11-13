using System.Security.Claims;
using DongNamTrip.Contracts;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Quotation;

/// <summary>Nhân viên xử lý yêu cầu tour riêng, xác minh và báo giá (UC17, UC18, SCR18). Không chứa quy tắc nghiệp vụ.</summary>
public static class QuotationAdminEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var staff = endpoints.MapGroup("/api/admin/quote-requests").WithTags("Quotation")
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Operator));

        staff.MapGet("", (string? status, DateTime? from, DateTime? to, int? page, int? size, QuotationAdminService service, CancellationToken ct) =>
                service.ListAsync(new QuoteRequestFilter(status, from, to, page, size), ct))
            .WithSummary("List private-tour requests for staff").Produces<PagedResult<QuoteRequestInfo>>();
        staff.MapGet("/{id:int}", async (int id, QuotationAdminService service, CancellationToken ct) =>
                await service.GetAsync(id, ct) is { } item ? Results.Ok(item) : Results.NotFound())
            .WithSummary("Get a private-tour request for staff").Produces<AdminQuoteRequestDetail>().Produces(StatusCodes.Status404NotFound);
        staff.MapPost("/{id:int}/receive", async (int id, ClaimsPrincipal user, QuotationAdminService service, CancellationToken ct) =>
                ToHttp(await service.ReceiveAsync(id, AccountId(user), ct)))
            .WithSummary("Receive a submitted request").Produces<QuoteRequestInfo>();
        staff.MapPost("/{id:int}/verifications", async (int id, VerificationForm form, ClaimsPrincipal user, QuotationAdminService service, CancellationToken ct) =>
                ToHttp(await service.RecordVerificationAsync(id, form, AccountId(user), ct), item => Results.Created($"/api/admin/quote-requests/{id}", item)))
            .WithSummary("Record a service verification").Produces<VerificationInfo>(StatusCodes.Status201Created);
        staff.MapPost("/{id:int}/quotes", async (int id, QuoteForm form, ClaimsPrincipal user, QuotationAdminService service, CancellationToken ct) =>
                ToHttp(await service.IssueQuoteAsync(id, form, AccountId(user), ct), item => Results.Created($"/api/admin/quote-requests/{id}", item)))
            .WithSummary("Issue a quote (replaces the previous sent quote)").Produces<QuoteInfo>(StatusCodes.Status201Created);
        staff.MapPost("/{id:int}/cancel", async (int id, CancelRequest request, ClaimsPrincipal user, QuotationAdminService service, CancellationToken ct) =>
                ToHttp(await service.CancelAsync(id, request.Reason, AccountId(user), ct)))
            .WithSummary("Cancel a private-tour request").Produces<QuoteRequestInfo>();
    }
}
