using System.Security.Claims;
using DongNamTrip.Contracts;
using Microsoft.AspNetCore.RateLimiting;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Administration;

/// <summary>Lớp giao diện (UI) của module Administration: ánh xạ HTTP endpoint. Không truy cập dữ liệu, không chứa quy tắc nghiệp vụ.</summary>
public static class AdministrationEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        MapAuthentication(endpoints.MapGroup("/api/auth").WithTags("Administration"));
        MapAccountArea(endpoints.MapGroup("/api/account").RequireAuthorization().WithTags("Administration"));
        // Compatibility aliases for existing clients; remove in a later release after migration to /api/admin/users, /api/admin/audit and /api/admin/stats.
        MapAdmin(endpoints.MapGroup("/api/admin/accounts").RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin)).WithTags("Administration"));
    }

    private static void MapAdmin(RouteGroupBuilder admin)
    {
        admin.MapGet("/", async (string? query, string? role, AdminAccountService service, CancellationToken ct) => Results.Ok(await service.AccountsAsync(query, role, ct)));
        admin.MapGet("/paged", async (string? group, string? query, string? role, string? status, int? page, AdminAccountService service, CancellationToken ct) =>
            ToHttp(await service.AccountPageAsync(group, query, role, status, page, ct)))
            .WithSummary("Danh sách tài khoản theo nhóm, bộ lọc và trang").Produces<AdminAccountPage>().Produces(400).Produces(401).Produces(403);
        admin.MapGet("/roles", async (AdminAccountService service, CancellationToken ct) => Results.Ok(await service.RolesAsync(ct)));
        admin.MapGet("/audit", async (int? page, string? actorType, DateOnly? from, DateOnly? to, string? query, AdminAccountService service, CancellationToken ct) =>
            ToHttp(await service.AuditAsync(page, actorType, from, to, query, ct)));
        admin.MapGet("/statistics", async (DateOnly? from, DateOnly? to, string? status, AdminAccountService service, CancellationToken ct) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var start = from ?? new DateOnly(today.Year, today.Month, 1);
            var end = to ?? new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
            return ToHttp(await service.StatisticsAsync(start, end, status, ct));
        }).WithSummary("Statistics by Vietnam business date and order status").Produces<AdminStatistics>().Produces(400).Produces(401).Produces(403);
        admin.MapPut("/{id:int}/roles", async (int id, AdminRoleUpdate input, ClaimsPrincipal user, AdminAccountService service, CancellationToken ct) => MutationResult(await service.RolesAsync(id, AccountId(user), input, ct)));
        admin.MapPut("/{id:int}/status", async (int id, AdminStatusUpdate input, ClaimsPrincipal user, AdminAccountService service, CancellationToken ct) => MutationResult(await service.StatusAsync(id, AccountId(user), input, ct)));
    }

    private static IResult MutationResult(AdminMutation result) => result switch
    {
        AdminMutation.Done => Results.NoContent(),
        AdminMutation.NotFound => Results.NotFound(),
        AdminMutation.Conflict => Results.Conflict(),
        _ => Results.BadRequest()
    };

    private static void MapAuthentication(RouteGroupBuilder auth)
    {
        auth.MapPost("/register", async (RegisterRequest input, SqlAuthService authentication, CancellationToken ct) =>
        {
            var result = await authentication.RegisterAsync(input, ct);
            return result.Status switch
            {
                RegistrationStatus.Created => Results.Created("/api/auth/me", result.Session),
                RegistrationStatus.Duplicate => Results.Conflict(),
                RegistrationStatus.Invalid => Results.BadRequest(),
                _ => Results.StatusCode(StatusCodes.Status503ServiceUnavailable)
            };
        }).RequireRateLimiting(RateLimits.Register)
          .WithSummary("Register a customer account")
          .WithDescription("Creates only the Customer role and returns an opaque session token. Staff roles cannot be self-registered.")
          .Produces<LoginResponse>(StatusCodes.Status201Created).Produces(StatusCodes.Status400BadRequest).Produces(StatusCodes.Status409Conflict).Produces(StatusCodes.Status503ServiceUnavailable);

        auth.MapPost("/login", async (LoginRequest input, SqlAuthService authentication, CancellationToken ct) =>
        {
            if (!Valid(input))
                return Results.Unauthorized(); // không tiết lộ lý do để tránh dò tài khoản
            var result = await authentication.LoginAsync(input, ct);
            return result is null ? Results.Unauthorized() : Results.Ok(result);
        }).RequireRateLimiting(RateLimits.Login)
          .WithSummary("Sign in")
          .WithDescription("Returns an opaque in-memory session token, not a JWT. Do not put credentials in examples.")
          .Produces<LoginResponse>().Produces(StatusCodes.Status401Unauthorized);

        var signedIn = auth.MapGroup("").RequireAuthorization();

        signedIn.MapPost("/logout", (HttpContext context, SqlSessionStore sessions) =>
        {
            var header = context.Request.Headers.Authorization.ToString();
            if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                sessions.Remove(header[7..].Trim());
            return Results.NoContent();
        }).Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status401Unauthorized);

        signedIn.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(CurrentUser(user)))
            .Produces<AuthenticatedUser>().Produces(StatusCodes.Status401Unauthorized);

        signedIn.MapGet("/profile", async (ClaimsPrincipal user, SqlAuthService authentication, CancellationToken ct) =>
            await authentication.GetProfileAsync(AccountId(user), ct) is { } profile ? Results.Ok(profile) : Results.Unauthorized())
            .Produces<AccountProfile>().Produces(StatusCodes.Status401Unauthorized);

        signedIn.MapPut("/profile", async (UpdateProfileRequest input, ClaimsPrincipal user, SqlAuthService authentication, CancellationToken ct) =>
        {
            var result = await authentication.UpdateProfileAsync(AccountId(user), input, ct);
            return result.Status switch
            {
                ProfileUpdateStatus.Updated => Results.Ok(result.Profile),
                ProfileUpdateStatus.Duplicate => Results.Conflict(),
                ProfileUpdateStatus.Invalid => Results.ValidationProblem(new Dictionary<string, string[]>(result.Errors!)),
                _ => Results.Unauthorized()
            };
        }).Produces<AccountProfile>().Produces(StatusCodes.Status400BadRequest).Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status409Conflict);

        signedIn.MapPost("/password", async (ChangePasswordRequest input, ClaimsPrincipal user, SqlAuthService authentication, CancellationToken ct) =>
            await authentication.ChangePasswordAsync(AccountId(user), input, ct) switch
            {
                PasswordChangeStatus.Changed => Results.NoContent(),
                PasswordChangeStatus.IncorrectCurrent => Results.Problem("Mật khẩu hiện tại không đúng.", statusCode: StatusCodes.Status422UnprocessableEntity),
                PasswordChangeStatus.Invalid => Results.BadRequest(),
                _ => Results.Unauthorized()
            }).Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status400BadRequest).Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status422UnprocessableEntity);

        signedIn.MapGet("/sessions", (ClaimsPrincipal user, HttpContext context, SqlSessionStore sessions) =>
            Results.Ok(sessions.ForAccount(AccountId(user), context.Request.Headers.Authorization.ToString()[7..].Trim())))
            .Produces<IReadOnlyList<AccountSession>>().Produces(StatusCodes.Status401Unauthorized);

        signedIn.MapDelete("/sessions/{id}", (string id, ClaimsPrincipal user, SqlSessionStore sessions) =>
            sessions.RemoveForAccount(AccountId(user), id) ? Results.NoContent() : Results.NotFound())
            .Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status404NotFound);
    }

    private static void MapAccountArea(RouteGroupBuilder account)
    {
        account.MapGet("/collections", async (ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
            Results.Ok(await service.CollectionsAsync(AccountId(user), ct))).Produces<AccountCollections>().Produces(StatusCodes.Status401Unauthorized);

        account.MapPut("/favorites/{kind}/{id:int}", async (string kind, int id, ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
            await service.AddFavoriteAsync(AccountId(user), kind, id, ct) ? Results.NoContent() : Results.NotFound()).Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status404NotFound);

        account.MapDelete("/favorites/{kind}/{id:int}", async (string kind, int id, ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
        {
            await service.RemoveFavoriteAsync(AccountId(user), kind, id, ct);
            return Results.NoContent();
        }).Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status401Unauthorized);

        account.MapPost("/activity", async (AccountActivityDraft input, ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
            await service.RecordActivityAsync(AccountId(user), input, ct) ? Results.NoContent() : Results.BadRequest()).Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status400BadRequest).Produces(StatusCodes.Status401Unauthorized);

        account.MapDelete("/activity/{id:long}", async (long id, ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
        {
            await service.RemoveActivityAsync(AccountId(user), id, ct);
            return Results.NoContent();
        }).Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status401Unauthorized);

        account.MapDelete("/activity", async (string kind, ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
            await service.ClearActivityAsync(AccountId(user), kind, ct) ? Results.NoContent() : Results.BadRequest()).Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status400BadRequest).Produces(StatusCodes.Status401Unauthorized);

        account.MapGet("/notices", async (ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
            Results.Ok(await service.NoticesAsync(AccountId(user), ct))).Produces<List<AccountNoticeEntry>>().Produces(StatusCodes.Status401Unauthorized);

        account.MapPut("/notices/{id:long}/read", async (long id, ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
            await service.MarkNoticeReadAsync(AccountId(user), id, ct) ? Results.NoContent() : Results.NotFound()).Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status404NotFound);

        account.MapGet("/preferences", async (ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
            Results.Ok(await service.PreferenceAsync(AccountId(user), ct))).Produces<AccountPreferenceDto>().Produces(StatusCodes.Status401Unauthorized);

        account.MapPut("/preferences", async (AccountPreferenceDto input, ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
            Results.Ok(await service.SavePreferenceAsync(AccountId(user), input, ct))).Produces<AccountPreferenceDto>().Produces(StatusCodes.Status401Unauthorized);
    }

    private static AuthenticatedUser CurrentUser(ClaimsPrincipal user) => new(
        AccountId(user),
        user.Identity?.Name ?? "",
        user.FindFirstValue(ClaimTypes.Email) ?? "",
        user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray());
}
