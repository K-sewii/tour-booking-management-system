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
app.MapPut("/api/auth/profile", async (UpdateProfileRequest input, ClaimsPrincipal user, SqlAuthService authentication, CancellationToken cancellationToken) =>
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

app.MapPost("/api/auth/password", async (ChangePasswordRequest input, ClaimsPrincipal user, SqlAuthService authentication, CancellationToken cancellationToken) =>
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
app.MapGet("/api/auth/sessions", (ClaimsPrincipal user, HttpContext context, SqlSessionStore sessions) =>
    Results.Ok(sessions.ForAccount(AccountId(user), context.Request.Headers.Authorization.ToString()[7..].Trim()))).RequireAuthorization();
app.MapDelete("/api/auth/sessions/{id}", (string id, ClaimsPrincipal user, SqlSessionStore sessions) =>
    sessions.RemoveForAccount(AccountId(user), id) ? Results.NoContent() : Results.NotFound()).RequireAuthorization();

app.MapGet("/api/account/payments", async (ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken cancellationToken) =>
{
    var owner = AccountId(user);
    var orders = await db.Dondat.AsNoTracking().Where(item => item.MaTk == owner)
        .Include(item => item.Giaodichtien).OrderByDescending(item => item.NgayDat).ToListAsync(cancellationToken);
    return Results.Ok(orders.Select(item => new PaymentEntry(item.MaDon, item.NgayDat, item.TrangThai, item.GiaChot, item.SoKhach, item.MaBg, item.MaLkh,
        item.Giaodichtien.OrderByDescending(entry => entry.ThoiDiem).Select(entry => new MoneyEntry(entry.MaGd, entry.LoaiGd, entry.SoTien, entry.ThoiDiem, entry.MaThamChieu)).ToArray())));
}).RequireAuthorization();
app.MapGet("/api/account/payment-choice", async (ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken ct) =>
{
    var owner = AccountId(user);
    var choice = await db.AccountPaymentChoices.AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == owner, ct);
    return Results.Ok(new PaymentChoiceDto(choice?.MethodCode, choice?.UpdatedAt));
}).RequireAuthorization();
app.MapPut("/api/account/payment-choice", async (PaymentChoiceRequest input, ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken ct) =>
{
    if (input.MethodCode is not ("bank_transfer" or "cash")) return Results.BadRequest();
    var owner = AccountId(user);
    var choice = await db.AccountPaymentChoices.SingleOrDefaultAsync(x => x.AccountId == owner, ct);
    if (choice is null)
    {
        choice = new() { AccountId = owner };
        db.AccountPaymentChoices.Add(choice);
    }
    choice.MethodCode = input.MethodCode;
    choice.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync(ct);
    return Results.Ok(new PaymentChoiceDto(choice.MethodCode, choice.UpdatedAt));
}).RequireAuthorization();
app.MapGet("/api/account/payments/{id:int}", async (int id, ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken cancellationToken) =>
{
    var owner = AccountId(user);
    var item = await db.Dondat.AsNoTracking().Include(order => order.Giaodichtien)
        .SingleOrDefaultAsync(order => order.MaDon == id && order.MaTk == owner, cancellationToken);
    return item is null ? Results.NotFound() : Results.Ok(new PaymentEntry(item.MaDon, item.NgayDat, item.TrangThai, item.GiaChot, item.SoKhach, item.MaBg, item.MaLkh,
        item.Giaodichtien.OrderByDescending(entry => entry.ThoiDiem).Select(entry => new MoneyEntry(entry.MaGd, entry.LoaiGd, entry.SoTien, entry.ThoiDiem, entry.MaThamChieu)).ToArray()));
}).RequireAuthorization();

app.MapGet("/api/account/collections", async (ClaimsPrincipal user, DongNamTripDbContext db, DemoStore catalog, CancellationToken ct) =>
{
    var owner = AccountId(user);
    var favorites = await db.AccountFavorites.AsNoTracking().Where(x => x.AccountId == owner).OrderByDescending(x => x.SavedAt).ToListAsync(ct);
    var activity = await db.AccountActivities.AsNoTracking().Where(x => x.AccountId == owner).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Take(200).ToListAsync(ct);
    string? Name(string kind, int? id) => kind == "tour" ? catalog.Tours().FirstOrDefault(x => x.Id == id)?.Name
        : kind == "destination" ? catalog.Destinations().FirstOrDefault(x => x.Id == id)?.Name : null;
    return Results.Ok(new AccountCollections(
        favorites.Select(x => new AccountCollectionItem(x.Kind, x.ItemId, Name(x.Kind, x.ItemId), x.SavedAt, Name(x.Kind, x.ItemId) is null)).ToArray(),
        activity.Where(x => x.Kind == "search").Select(x => new AccountActivityEntry(x.Id, x.Kind, null, x.Query, null, x.CreatedAt, false)).ToArray(),
        activity.Where(x => x.Kind != "search").Select(x => new AccountActivityEntry(x.Id, x.Kind, x.ItemId, null, Name(x.Kind, x.ItemId), x.CreatedAt, Name(x.Kind, x.ItemId) is null)).ToArray()));
}).RequireAuthorization();
app.MapPut("/api/account/favorites/{kind}/{id:int}", async (string kind, int id, ClaimsPrincipal user, DongNamTripDbContext db, DemoStore catalog, CancellationToken ct) =>
{
    if (!CatalogHas(catalog, kind, id)) return Results.NotFound();
    var owner = AccountId(user);
    if (!await db.AccountFavorites.AnyAsync(x => x.AccountId == owner && x.Kind == kind && x.ItemId == id, ct))
    {
        db.AccountFavorites.Add(new() { AccountId = owner, Kind = kind, ItemId = id, SavedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
    }
    return Results.NoContent();
}).RequireAuthorization();
app.MapDelete("/api/account/favorites/{kind}/{id:int}", async (string kind, int id, ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken ct) =>
{
    var owner = AccountId(user);
    await db.AccountFavorites.Where(x => x.AccountId == owner && x.Kind == kind && x.ItemId == id).ExecuteDeleteAsync(ct);
    return Results.NoContent();
}).RequireAuthorization();
app.MapPost("/api/account/activity", async (AccountActivityDraft input, ClaimsPrincipal user, DongNamTripDbContext db, DemoStore catalog, CancellationToken ct) =>
{
    var query = input.Query?.Trim();
    if (input.Kind == "search" ? string.IsNullOrEmpty(query) || query.Length > 200 || input.ItemId is not null
        : input.ItemId is null || !CatalogHas(catalog, input.Kind, input.ItemId.Value) || query is not null)
        return Results.BadRequest();
    db.AccountActivities.Add(new() { AccountId = AccountId(user), Kind = input.Kind, ItemId = input.ItemId, Query = query, CreatedAt = DateTime.UtcNow });
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
}).RequireAuthorization();
app.MapDelete("/api/account/activity/{id:long}", async (long id, ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken ct) =>
{
    var owner = AccountId(user);
    await db.AccountActivities.Where(x => x.Id == id && x.AccountId == owner).ExecuteDeleteAsync(ct);
    return Results.NoContent();
}).RequireAuthorization();
app.MapDelete("/api/account/activity", async (string kind, ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken ct) =>
{
    if (kind is not ("search" or "viewed")) return Results.BadRequest();
    var owner = AccountId(user);
    await db.AccountActivities.Where(x => x.AccountId == owner && (kind == "search" ? x.Kind == "search" : x.Kind != "search")).ExecuteDeleteAsync(ct);
    return Results.NoContent();
}).RequireAuthorization();
app.MapGet("/api/account/notices", async (ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken ct) =>
{
    var owner = AccountId(user);
    return Results.Ok(await db.AccountNotices.AsNoTracking().Where(x => x.AccountId == owner).OrderByDescending(x => x.CreatedAt).Select(x => new AccountNoticeEntry(x.Id, x.Title, x.Body, x.CreatedAt, x.ReadAt)).ToListAsync(ct));
}).RequireAuthorization();
app.MapPut("/api/account/notices/{id:long}/read", async (long id, ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken ct) =>
{
    var owner = AccountId(user);
    var now = DateTime.UtcNow;
    var changed = await db.AccountNotices.Where(x => x.Id == id && x.AccountId == owner && x.ReadAt == null).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ReadAt, now), ct);
    return changed > 0 ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization();
app.MapGet("/api/account/preferences", async (ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken ct) =>
{
    var owner = AccountId(user);
    return Results.Ok(new AccountPreferenceDto((await db.AccountPreferences.AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == owner, ct))?.SecurityNotices ?? true));
}).RequireAuthorization();
app.MapPut("/api/account/preferences", async (AccountPreferenceDto input, ClaimsPrincipal user, DongNamTripDbContext db, CancellationToken ct) =>
{
    var owner = AccountId(user);
    var current = await db.AccountPreferences.SingleOrDefaultAsync(x => x.AccountId == owner, ct);
    if (current is null) db.AccountPreferences.Add(new() { AccountId = owner, SecurityNotices = input.SecurityNotices });
    else current.SecurityNotices = input.SecurityNotices;
    await db.SaveChangesAsync(ct);
    return Results.Ok(input);
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
static bool CatalogHas(DemoStore catalog, string kind, int id) => kind == "tour" ? catalog.Tours().Any(item => item.Id == id)
    : kind == "destination" && catalog.Destinations().Any(item => item.Id == id);
static string OwnerKey(ClaimsPrincipal user) => $"account:{AccountId(user)}";
static int AccountId(ClaimsPrincipal user) => int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
static AuthenticatedUser CurrentUser(ClaimsPrincipal user) => new(
    int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!),
    user.Identity?.Name ?? "",
    user.FindFirstValue(ClaimTypes.Email) ?? "",
    user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray());
