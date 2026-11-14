using System.Security.Claims;
using DongNamTrip.Contracts;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Administration;

/// <summary>Quản lý người dùng, nhật ký và thống kê (UC24, SCR13, SCR19). Chỉ Admin. Không chứa quy tắc nghiệp vụ.</summary>
public static class UserAdminEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin").WithTags("Administration").RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin));

        var users = admin.MapGroup("/users");
        users.MapGet("/grouped", async (string? group, string? q, string? role, string? status, int? page, UserAdminService service, CancellationToken ct) =>
                ToHttp(await service.GroupedAsync(group, q, role, status, page, ct)))
            .WithSummary("List accounts by group with counters").Produces<AdminAccountPage>();
        users.MapGet("/roles", (UserAdminService service, CancellationToken ct) => service.RolesAsync(ct));
        users.MapGet("", (string? q, string? role, string? status, int? page, int? size, UserAdminService service, CancellationToken ct) =>
                service.ListAsync(new UserFilter(q, role, status, page, size), ct))
            .WithSummary("List users").Produces<PagedResult<AdminUser>>();
        users.MapGet("/{id:int}", async (int id, UserAdminService service, CancellationToken ct) =>
                await service.GetAsync(id, ct) is { } item ? Results.Ok(item) : Results.NotFound())
            .WithSummary("Get a user").Produces<AdminUserDetail>().Produces(StatusCodes.Status404NotFound);
        users.MapPost("", async (StaffForm form, ClaimsPrincipal user, UserAdminService service, HttpContext http, CancellationToken ct) =>
            {
                http.Response.Headers.CacheControl = "no-store"; // phản hồi chứa mật khẩu tạm
                return ToHttp(await service.CreateStaffAsync(form, AccountId(user), ct), item => Results.Created($"/api/admin/users/{item.User.Id}", item));
            })
            .WithSummary("Create a staff account with a temporary password").Produces<StaffCreated>(StatusCodes.Status201Created);
        users.MapPut("/{id:int}/roles", async (int id, RolesRequest request, ClaimsPrincipal user, UserAdminService service, CancellationToken ct) =>
                ToHttp(await service.SetRolesAsync(id, request.Roles, AccountId(user), ct, request.ExpectedRoles)))
            .WithSummary("Replace the roles of an account").Produces<AdminUser>();
        users.MapPost("/{id:int}/status", async (int id, StatusRequest request, ClaimsPrincipal user, UserAdminService service, CancellationToken ct) =>
                ToHttp(await service.SetStatusAsync(id, request.Status, request.Reason, AccountId(user), ct, request.ExpectedStatus)))
            .WithSummary("Lock or unlock an account").Produces<AdminUser>();
        users.MapPost("/{id:int}/reset-password", async (int id, ClaimsPrincipal user, UserAdminService service, HttpContext http, CancellationToken ct) =>
            {
                http.Response.Headers.CacheControl = "no-store"; // phản hồi chứa mật khẩu tạm
                return ToHttp(await service.ResetPasswordAsync(id, AccountId(user), ct));
            })
            .WithSummary("Reset a password to a temporary one").Produces<TempPassword>();

        admin.MapGet("/audit/search", async (int? page, string? actorType, DateOnly? from, DateOnly? to, string? query, AuditService service, CancellationToken ct) =>
                ToHttp(await service.QueryScreenAsync(page, actorType, from, to, query, ct)))
            .WithSummary("Read the SCR19 audit log").Produces<AdminAuditPage>();
        admin.MapGet("/audit", async (DateTime? from, DateTime? to, string? objectType, int? actorId, int? page, int? size, AuditService service, CancellationToken ct) =>
                ToHttp(await service.QueryAsync(new AuditFilter(from, to, objectType, actorId, page, size), ct)))
            .WithSummary("Read the audit log (read-only)").Produces<PagedResult<AuditRow>>().Produces(StatusCodes.Status400BadRequest);
        admin.MapGet("/stats", async (DateTime? from, DateTime? to, string? status, StatisticsService service, CancellationToken ct) =>
                ToHttp(await service.SummaryAsync(from ?? BusinessDate.Today.AddDays(-30), to ?? BusinessDate.Today, status, ct)))
            .WithSummary("Operation statistics").Produces<OperationSummary>().Produces(StatusCodes.Status400BadRequest);
    }
}
