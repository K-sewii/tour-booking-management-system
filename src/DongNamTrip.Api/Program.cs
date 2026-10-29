using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.RateLimiting;
using DongNamTrip.Api;
using DongNamTrip.Api.Data;
using DongNamTrip.Api.Data.Entities;
using DongNamTrip.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Bản nền tảng chỉ hỗ trợ môi trường Development. Thay dịch vụ demo và xác thực trước khi triển khai thật.");

var connectionString = builder.Configuration.GetConnectionString("DongNamTrip")
    ?? throw new InvalidOperationException("ConnectionStrings:DongNamTrip must be configured for the API.");
builder.Services.AddDbContext<DongNamTripDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.Configure<SqlAuthenticationOptions>(builder.Configuration.GetSection(SqlAuthenticationOptions.SectionName));
builder.Services.AddSingleton<SqlSessionStore>();
builder.Services.AddSingleton<IPasswordHasher<Taikhoan>, PasswordHasher<Taikhoan>>();
builder.Services.AddScoped<SqlAuthService>();
builder.Services.AddSingleton<DevelopmentAdminBootstrapper>();
builder.Services.AddSingleton<DevelopmentTestAccountsBootstrapper>();
builder.Services.AddSingleton<DemoStore>();
builder.Services.AddAuthentication("SqlBearer").AddScheme<AuthenticationSchemeOptions, SqlAuthenticationHandler>("SqlBearer", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = 20,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0
    }));
});

var app = builder.Build();
await app.Services.GetRequiredService<DevelopmentAdminBootstrapper>().EnsureCreatedAsync(app.Environment);
await app.Services.GetRequiredService<DevelopmentTestAccountsBootstrapper>().EnsureCreatedAsync(app.Environment);

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok", mode = "sql-auth-demo-catalog" }));
app.MapGet("/health/db", async (DongNamTripDbContext db, ILogger<Program> logger, CancellationToken cancellationToken) =>
{
    try
    {
        if (!await db.Database.CanConnectAsync(cancellationToken))
            return Results.Json(new { status = "unavailable" }, statusCode: StatusCodes.Status503ServiceUnavailable);

        var vaiTroCount = await db.Vaitro.AsNoTracking().CountAsync(cancellationToken);
        var ngonNguCount = await db.Ngonngu.AsNoTracking().CountAsync(cancellationToken);
        var tableCount = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.tables WHERE is_ms_shipped = 0").SingleAsync(cancellationToken);
        return Results.Ok(new { status = "ok", tableCount, vaiTroCount, ngonNguCount });
    }
    catch (Exception exception)
    {
        logger.LogWarning(exception, "Database health check failed.");
        return Results.Json(new { status = "unavailable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapPost("/api/auth/register", async (RegisterRequest input, SqlAuthService authentication, CancellationToken cancellationToken) =>
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

app.MapPost("/api/auth/login", async (LoginRequest input, SqlAuthService authentication, CancellationToken cancellationToken) =>
{
    if (!Valid(input))
        return Results.Unauthorized();

    var result = await authentication.LoginAsync(input, cancellationToken);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
}).RequireRateLimiting("login");

app.MapPost("/api/auth/logout", (HttpContext context, SqlSessionStore sessions) =>
{
    var header = context.Request.Headers.Authorization.ToString();
    if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        sessions.Remove(header[7..].Trim());
    return Results.NoContent();
}).RequireAuthorization();

app.MapGet("/api/auth/me", (ClaimsPrincipal user) => Results.Ok(CurrentUser(user))).RequireAuthorization();
app.MapGet("/api/auth/profile", async (ClaimsPrincipal user, SqlAuthService authentication, CancellationToken cancellationToken) =>
{
    var profile = await authentication.GetProfileAsync(AccountId(user), cancellationToken);
    return profile is null ? Results.Unauthorized() : Results.Ok(profile);
}).RequireAuthorization();
app.MapPut("/api/auth/profile", async (UpdateProfileRequest input, ClaimsPrincipal user, SqlAuthService authentication, DemoStore store, CancellationToken cancellationToken) =>
{
    var result = await authentication.UpdateProfileAsync(AccountId(user), input, cancellationToken);
    return result.Status switch
    {
        ProfileUpdateStatus.Updated => UpdateProfileResult(result, store),
        ProfileUpdateStatus.Duplicate => Results.Conflict(),
        ProfileUpdateStatus.Invalid => Results.BadRequest(),
        _ => Results.Unauthorized()
    };
}).RequireAuthorization();

app.MapGet("/api/destinations", (DemoStore store) => store.Destinations());
app.MapGet("/api/tours", (DemoStore store) => store.Tours());
app.MapPost("/api/destinations", (DestinationDraft draft, DemoStore store) =>
    ValidDestination(draft) ? Results.Ok(store.Save(null, draft)) : Results.BadRequest()).RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Editor));
app.MapPut("/api/destinations/{id:int}", (int id, DestinationDraft draft, DemoStore store) =>
    !ValidDestination(draft) ? Results.BadRequest() : store.Save(id, draft) is { } result ? Results.Ok(result) : Results.NotFound()).RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Editor));

app.MapGet("/api/itineraries", (ClaimsPrincipal user, DemoStore store) =>
    store.Itineraries(OwnerKey(user))).RequireAuthorization();
app.MapPost("/api/itineraries", (ItineraryDraft draft, ClaimsPrincipal user, DemoStore store) =>
{
    if (!Valid(draft) || draft.StartDate < DateOnly.FromDateTime(DateTime.Today) || draft.DestinationIds.Distinct().Count() != draft.DestinationIds.Length)
        return Results.BadRequest();

    var item = store.SaveItinerary(OwnerKey(user), int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!), draft);
    return item is null ? Results.BadRequest() : Results.Ok(item);
}).RequireAuthorization();

app.Run();

static bool Valid(object value) => Validator.TryValidateObject(value, new ValidationContext(value), [], true);
static bool ValidDestination(DestinationDraft draft) => Valid(draft) && CatalogOptions.Regions.Contains(draft.Region) && CatalogOptions.Categories.Contains(draft.Category);
static string OwnerKey(ClaimsPrincipal user) => SqlAuthService.NormalizeEmail(user.FindFirstValue(ClaimTypes.Email));
static int AccountId(ClaimsPrincipal user) => int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
static IResult UpdateProfileResult(ProfileUpdateResult result, DemoStore store)
{
    store.MoveItineraries(result.PreviousEmail!, result.Profile!.Email);
    return Results.Ok(result.Profile);
}
static AuthenticatedUser CurrentUser(ClaimsPrincipal user) => new(
    int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!),
    user.Identity?.Name ?? "",
    user.FindFirstValue(ClaimTypes.Email) ?? "",
    user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray());
