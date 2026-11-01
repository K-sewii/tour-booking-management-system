using System.Security.Claims;
using DongNamTrip.Modules.Persistence;
using DongNamTrip.Modules.Persistence.Entities;
using DongNamTrip.Modules.Catalog;
using DongNamTrip.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using static DongNamTrip.Modules.ModuleHttp;

namespace DongNamTrip.Modules.Administration;

/// <summary>Lớp giao diện (UI) của module Administration: ánh xạ HTTP endpoint.</summary>
public static class AdministrationEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
endpoints.MapPost("/api/auth/register", async (RegisterRequest input, SqlAuthService authentication, CancellationToken cancellationToken) =>
{
    var result = await authentication.RegisterAsync(input, cancellationToken);
    return result.Status switch
    {
        RegistrationStatus.Created => Results.Created("/api/auth/me", result.Session),
        RegistrationStatus.Duplicate => Results.Conflict(),
        RegistrationStatus.Invalid => Results.BadRequest(),
        _ => Results.StatusCode(StatusCodes.Status503ServiceUnavailable)
    };
});

endpoints.MapPost("/api/auth/login", async (LoginRequest input, SqlAuthService authentication, CancellationToken cancellationToken) =>
{
    if (!Valid(input))
        return Results.Unauthorized();

    var result = await authentication.LoginAsync(input, cancellationToken);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
}).RequireRateLimiting("login");

endpoints.MapPost("/api/auth/logout", (HttpContext context, SqlSessionStore sessions) =>
{
    var header = context.Request.Headers.Authorization.ToString();
    if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        sessions.Remove(header[7..].Trim());
    return Results.NoContent();
}).RequireAuthorization();

endpoints.MapGet("/api/auth/me", (ClaimsPrincipal user) => Results.Ok(CurrentUser(user))).RequireAuthorization();
endpoints.MapGet("/api/auth/profile", async (ClaimsPrincipal user, SqlAuthService authentication, CancellationToken cancellationToken) =>
{
    var profile = await authentication.GetProfileAsync(AccountId(user), cancellationToken);
    return profile is null ? Results.Unauthorized() : Results.Ok(profile);
}).RequireAuthorization();
endpoints.MapPut("/api/auth/profile", async (UpdateProfileRequest input, ClaimsPrincipal user, SqlAuthService authentication, CancellationToken cancellationToken) =>
{
    var result = await authentication.UpdateProfileAsync(AccountId(user), input, cancellationToken);
    if (result.Status == ProfileUpdateStatus.Invalid)
    {
        var existing = await authentication.GetProfileAsync(AccountId(user), cancellationToken);
        var validation = input.ValidateForExistingPhone(existing?.PhoneNumber);
        return Results.ValidationProblem(validation
            .SelectMany(issue => issue.MemberNames.Select(member => (member, issue.ErrorMessage)))
            .GroupBy(item => item.member)
            .ToDictionary(group => group.Key, group => group.Select(item => item.ErrorMessage!).ToArray()));
    }
    return result.Status switch
    {
        ProfileUpdateStatus.Updated => Results.Ok(result.Profile),
        ProfileUpdateStatus.Duplicate => Results.Conflict(),
        _ => Results.Unauthorized()
    };
}).RequireAuthorization();

endpoints.MapPost("/api/auth/password", async (ChangePasswordRequest input, ClaimsPrincipal user, SqlAuthService authentication, CancellationToken cancellationToken) =>
{
    var result = await authentication.ChangePasswordAsync(AccountId(user), input, cancellationToken);
    return result switch
    {
        PasswordChangeStatus.Changed => Results.NoContent(),
        PasswordChangeStatus.IncorrectCurrent => Results.Problem("Mật khẩu hiện tại không đúng.", statusCode: 422),
        PasswordChangeStatus.Invalid => Results.BadRequest(),
        _ => Results.Unauthorized()
    };
}).RequireAuthorization();
endpoints.MapGet("/api/auth/sessions", (ClaimsPrincipal user, HttpContext context, SqlSessionStore sessions) =>
    Results.Ok(sessions.ForAccount(AccountId(user), context.Request.Headers.Authorization.ToString()[7..].Trim()))).RequireAuthorization();
endpoints.MapDelete("/api/auth/sessions/{id}", (string id, ClaimsPrincipal user, SqlSessionStore sessions) =>
    sessions.RemoveForAccount(AccountId(user), id) ? Results.NoContent() : Results.NotFound()).RequireAuthorization();

endpoints.MapGet("/api/account/collections", async (ClaimsPrincipal user, DongNamTripDbContext db, ICatalogApi catalog, CancellationToken ct) =>
{
    var owner = AccountId(user);
    var favorites = await db.AccountFavorites.AsNoTracking().Where(x => x.AccountId == owner).OrderByDescending(x => x.SavedAt).ToListAsync(ct);
    var activity = await db.AccountActivities.AsNoTracking().Where(x => x.AccountId == owner).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Take(200).ToListAsync(ct);
    int[] Ids(string kind) => [.. favorites.Where(x => x.Kind == kind).Select(x => x.ItemId)
        .Concat(activity.Where(x => x.Kind == kind && x.ItemId != null).Select(x => x.ItemId!.Value)).Distinct()];
    var tourNames = await catalog.NamesAsync("tour", Ids("tour"), ct);
    var placeNames = await catalog.NamesAsync("destination", Ids("destination"), ct);
    string? Name(string kind, int? id) => id is not null && (kind == "tour" ? tourNames : kind == "destination" ? placeNames : []).TryGetValue(id.Value, out var name) ? name : null;
    return Results.Ok(new AccountCollections(
        favorites.Select(x => new AccountCollectionItem(x.Kind, x.ItemId, Name(x.Kind, x.ItemId), x.SavedAt, Name(x.Kind, x.ItemId) is null)).ToArray(),
        activity.Where(x => x.Kind == "search").Select(x => new AccountActivityEntry(x.Id, x.Kind, null, x.Query, null, x.CreatedAt, false)).ToArray(),
        activity.Where(x => x.Kind != "search").Select(x => new AccountActivityEntry(x.Id, x.Kind, x.ItemId, null, Name(x.Kind, x.ItemId), x.CreatedAt, Name(x.Kind, x.ItemId) is null)).ToArray()));
}).RequireAuthorization();
endpoints.MapPut("/api/account/favorites/{kind}/{id:int}", async (string kind, int id, ClaimsPrincipal user, DongNamTripDbContext db, ICatalogApi catalog, CancellationToken ct) =>
{
    if (!await catalog.ExistsAsync(kind, id, ct)) return Results.NotFound();
    var owner = AccountId(user);
    if (!await db.AccountFavorites.AnyAsync(x => x.AccountId == owner && x.Kind == kind && x.ItemId == id, ct))
    {
        db.AccountFavorites.Add(new() { AccountId = owner, Kind = kind, ItemId = id, SavedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
    }
    return Results.NoContent();
}).RequireAuthorization();
endpoints.MapDelete("/api/account/favorites/{kind}/{id:int}", async (string kind, int id, ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken ct) =>
{
    var owner = AccountId(user);
    await db.AccountFavorites.Where(x => x.AccountId == owner && x.Kind == kind && x.ItemId == id).ExecuteDeleteAsync(ct);
    return Results.NoContent();
}).RequireAuthorization();
endpoints.MapPost("/api/account/activity", async (AccountActivityDraft input, ClaimsPrincipal user, DongNamTripDbContext db, ICatalogApi catalog, CancellationToken ct) =>
{
    var query = input.Query?.Trim();
    if (input.Kind == "search" ? string.IsNullOrEmpty(query) || query.Length > 200 || input.ItemId is not null
        : input.ItemId is null || !await catalog.ExistsAsync(input.Kind, input.ItemId.Value, ct) || query is not null)
        return Results.BadRequest();
    db.AccountActivities.Add(new() { AccountId = AccountId(user), Kind = input.Kind, ItemId = input.ItemId, Query = query, CreatedAt = DateTime.UtcNow });
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
}).RequireAuthorization();
endpoints.MapDelete("/api/account/activity/{id:long}", async (long id, ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken ct) =>
{
    var owner = AccountId(user);
    await db.AccountActivities.Where(x => x.Id == id && x.AccountId == owner).ExecuteDeleteAsync(ct);
    return Results.NoContent();
}).RequireAuthorization();
endpoints.MapDelete("/api/account/activity", async (string kind, ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken ct) =>
{
    if (kind is not ("search" or "viewed")) return Results.BadRequest();
    var owner = AccountId(user);
    await db.AccountActivities.Where(x => x.AccountId == owner && (kind == "search" ? x.Kind == "search" : x.Kind != "search")).ExecuteDeleteAsync(ct);
    return Results.NoContent();
}).RequireAuthorization();
endpoints.MapGet("/api/account/notices", async (ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken ct) =>
{
    var owner = AccountId(user);
    return Results.Ok(await db.AccountNotices.AsNoTracking().Where(x => x.AccountId == owner).OrderByDescending(x => x.CreatedAt).Select(x => new AccountNoticeEntry(x.Id, x.Title, x.Body, x.CreatedAt, x.ReadAt)).ToListAsync(ct));
}).RequireAuthorization();
endpoints.MapPut("/api/account/notices/{id:long}/read", async (long id, ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken ct) =>
{
    var owner = AccountId(user);
    var now = DateTime.UtcNow;
    var changed = await db.AccountNotices.Where(x => x.Id == id && x.AccountId == owner && x.ReadAt == null).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ReadAt, now), ct);
    return changed > 0 ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization();
endpoints.MapGet("/api/account/preferences", async (ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken ct) =>
{
    var owner = AccountId(user);
    return Results.Ok(new AccountPreferenceDto((await db.AccountPreferences.AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == owner, ct))?.SecurityNotices ?? true));
}).RequireAuthorization();
endpoints.MapPut("/api/account/preferences", async (AccountPreferenceDto input, ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken ct) =>
{
    var owner = AccountId(user);
    var current = await db.AccountPreferences.SingleOrDefaultAsync(x => x.AccountId == owner, ct);
    if (current is null) db.AccountPreferences.Add(new() { AccountId = owner, SecurityNotices = input.SecurityNotices });
    else current.SecurityNotices = input.SecurityNotices;
    await db.SaveChangesAsync(ct);
    return Results.Ok(input);
}).RequireAuthorization();
    }



    private static AuthenticatedUser CurrentUser(ClaimsPrincipal user) => new(
        AccountId(user),
        user.Identity?.Name ?? "",
        user.FindFirstValue(ClaimTypes.Email) ?? "",
        user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray());
}
