using System.Threading.RateLimiting;
using DongNamTrip.Modules;
using DongNamTrip.Modules.Administration;
using DongNamTrip.Modules.Booking;
using DongNamTrip.Modules.Catalog;
using DongNamTrip.Modules.Itinerary;
using DongNamTrip.Modules.Narration;
using DongNamTrip.Modules.Quotation;
using DongNamTrip.Modules.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Bản nền tảng chỉ hỗ trợ môi trường Development. Thay dịch vụ demo và xác thực trước khi triển khai thật.");

var connectionString = builder.Configuration.GetConnectionString("DongNamTrip")
    ?? throw new InvalidOperationException("ConnectionStrings:DongNamTrip must be configured for the API.");
builder.Services.AddDbContext<DongNamTripDbContext>(options => options.UseSqlServer(connectionString));
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

IModule[] modules = [new AdministrationModule(), new CatalogModule(), new ItineraryModule(), new BookingModule(), new QuotationModule(), new NarrationModule()];
foreach (var module in modules)
    module.Register(builder.Services, builder.Configuration);

var app = builder.Build();
await AdministrationModule.EnsureDevelopmentAccountsAsync(app.Services, app.Environment);

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

foreach (var module in modules)
    module.MapEndpoints(app);

app.Run();
