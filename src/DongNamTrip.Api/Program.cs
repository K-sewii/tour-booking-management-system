using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DongNamTrip.Api;
using DongNamTrip.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Bản nền tảng chỉ hỗ trợ môi trường Development. Thay dịch vụ demo và xác thực trước khi triển khai thật.");
builder.Services.AddSingleton<DemoStore>();
builder.Services.AddSingleton<DemoSessions>();
builder.Services.AddAuthentication("DemoBearer").AddScheme<AuthenticationSchemeOptions, DemoAuthenticationHandler>("DemoBearer", _ =>
{
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options => options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })));
var app = builder.Build();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok", mode = "demo-memory" }));
app.MapPost("/api/auth/login", (LoginRequest input, DemoSessions sessions) => !Valid(input) ? Results.BadRequest() : sessions.Login(input)is { } result ? Results.Ok(result) : Results.Unauthorized()).RequireRateLimiting("login");
app.MapPost("/api/auth/logout", (HttpContext context, DemoSessions sessions) =>
{
    sessions.Remove(context.Request.Headers.Authorization.ToString()[7..]);
    return Results.NoContent();
}).RequireAuthorization();
app.MapGet("/api/destinations", (DemoStore store) => store.Destinations());
app.MapGet("/api/tours", (DemoStore store) => store.Tours());
app.MapPost("/api/destinations", (DestinationDraft d, DemoStore store) => ValidDestination(d) ? Results.Ok(store.Save(null, d)) : Results.BadRequest()).RequireAuthorization(policy => policy.RequireRole("Admin"));
app.MapPut("/api/destinations/{id:int}", (int id, DestinationDraft d, DemoStore store) => !ValidDestination(d) ? Results.BadRequest() : store.Save(id, d)is { } result ? Results.Ok(result) : Results.NotFound()).RequireAuthorization(policy => policy.RequireRole("Admin"));
app.MapGet("/api/itineraries", (ClaimsPrincipal user, DemoStore store) => store.Itineraries(int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!))).RequireAuthorization();
app.MapPost("/api/itineraries", (ItineraryDraft d, ClaimsPrincipal user, DemoStore store) =>
{
    if (!Valid(d) || d.StartDate < DateOnly.FromDateTime(DateTime.Today) || d.DestinationIds.Distinct().Count() != d.DestinationIds.Length)
        return Results.BadRequest();
    var item = store.SaveItinerary(int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!), d);
    return item is null ? Results.BadRequest() : Results.Ok(item);
}).RequireAuthorization();
app.Run();
static bool Valid(object value) => Validator.TryValidateObject(value, new ValidationContext(value), [], true);
static bool ValidDestination(DestinationDraft d) => Valid(d) && CatalogOptions.Regions.Contains(d.Region) && CatalogOptions.Categories.Contains(d.Category);
