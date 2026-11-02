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
        MapAuthentication(endpoints.MapGroup("/api/auth"));
        MapAccountArea(endpoints.MapGroup("/api/account").RequireAuthorization());
    }

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
        }).RequireRateLimiting(RateLimits.Register);

        auth.MapPost("/login", async (LoginRequest input, SqlAuthService authentication, CancellationToken ct) =>
        {
            if (!Valid(input))
                return Results.Unauthorized(); // không tiết lộ lý do để tránh dò tài khoản
            var result = await authentication.LoginAsync(input, ct);
            return result is null ? Results.Unauthorized() : Results.Ok(result);
        }).RequireRateLimiting(RateLimits.Login);

        var signedIn = auth.MapGroup("").RequireAuthorization();

        signedIn.MapPost("/logout", (HttpContext context, SqlSessionStore sessions) =>
        {
            var header = context.Request.Headers.Authorization.ToString();
            if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                sessions.Remove(header[7..].Trim());
            return Results.NoContent();
        });

        signedIn.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(CurrentUser(user)));

        signedIn.MapGet("/profile", async (ClaimsPrincipal user, SqlAuthService authentication, CancellationToken ct) =>
            await authentication.GetProfileAsync(AccountId(user), ct) is { } profile ? Results.Ok(profile) : Results.Unauthorized());

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
        });

        signedIn.MapPost("/password", async (ChangePasswordRequest input, ClaimsPrincipal user, SqlAuthService authentication, CancellationToken ct) =>
            await authentication.ChangePasswordAsync(AccountId(user), input, ct) switch
            {
                PasswordChangeStatus.Changed => Results.NoContent(),
                PasswordChangeStatus.IncorrectCurrent => Results.Problem("Mật khẩu hiện tại không đúng.", statusCode: StatusCodes.Status422UnprocessableEntity),
                PasswordChangeStatus.Invalid => Results.BadRequest(),
                _ => Results.Unauthorized()
            });

        signedIn.MapGet("/sessions", (ClaimsPrincipal user, HttpContext context, SqlSessionStore sessions) =>
            Results.Ok(sessions.ForAccount(AccountId(user), context.Request.Headers.Authorization.ToString()[7..].Trim())));

        signedIn.MapDelete("/sessions/{id}", (string id, ClaimsPrincipal user, SqlSessionStore sessions) =>
            sessions.RemoveForAccount(AccountId(user), id) ? Results.NoContent() : Results.NotFound());
    }

    private static void MapAccountArea(RouteGroupBuilder account)
    {
        account.MapGet("/collections", async (ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
            Results.Ok(await service.CollectionsAsync(AccountId(user), ct)));

        account.MapPut("/favorites/{kind}/{id:int}", async (string kind, int id, ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
            await service.AddFavoriteAsync(AccountId(user), kind, id, ct) ? Results.NoContent() : Results.NotFound());

        account.MapDelete("/favorites/{kind}/{id:int}", async (string kind, int id, ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
        {
            await service.RemoveFavoriteAsync(AccountId(user), kind, id, ct);
            return Results.NoContent();
        });

        account.MapPost("/activity", async (AccountActivityDraft input, ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
            await service.RecordActivityAsync(AccountId(user), input, ct) ? Results.NoContent() : Results.BadRequest());

        account.MapDelete("/activity/{id:long}", async (long id, ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
        {
            await service.RemoveActivityAsync(AccountId(user), id, ct);
            return Results.NoContent();
        });

        account.MapDelete("/activity", async (string kind, ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
            await service.ClearActivityAsync(AccountId(user), kind, ct) ? Results.NoContent() : Results.BadRequest());

        account.MapGet("/notices", async (ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
            Results.Ok(await service.NoticesAsync(AccountId(user), ct)));

        account.MapPut("/notices/{id:long}/read", async (long id, ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
            await service.MarkNoticeReadAsync(AccountId(user), id, ct) ? Results.NoContent() : Results.NotFound());

        account.MapGet("/preferences", async (ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
            Results.Ok(await service.PreferenceAsync(AccountId(user), ct)));

        account.MapPut("/preferences", async (AccountPreferenceDto input, ClaimsPrincipal user, AccountAreaService service, CancellationToken ct) =>
            Results.Ok(await service.SavePreferenceAsync(AccountId(user), input, ct)));
    }

    private static AuthenticatedUser CurrentUser(ClaimsPrincipal user) => new(
        AccountId(user),
        user.Identity?.Name ?? "",
        user.FindFirstValue(ClaimTypes.Email) ?? "",
        user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray());
}
