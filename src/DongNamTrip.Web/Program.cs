using DongNamTrip.Client;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Bản nền tảng demo chỉ chạy ở Development.");
builder.Services.AddControllersWithViews(options => options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddHttpClient<TripApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5080/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient("spa-proxy", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5080/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromHours(4);
    options.SlidingExpiration = false;
    options.Cookie.Name = "DongNamTrip.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
var app = builder.Build();
var spaEnabled = builder.Configuration.GetValue<bool?>("Spa:Enabled") ?? true;
var spaIndex = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "spa", "index.html");
if (!File.Exists(spaIndex))
    spaIndex = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "wwwroot", "spa", "index.html"));
if (spaEnabled && !File.Exists(spaIndex))
    throw new InvalidOperationException("Chưa tìm thấy bản build SPA. Chạy npm ci và npm run build trong src/DongNamTrip.Spa trước khi khởi động Web.");
app.UseExceptionHandler("/Home/Error");
app.UseWhen(context => context.Request.Path.StartsWithSegments("/spa-api"), branch =>
    branch.UseStatusCodePages(async statusContext =>
    {
        statusContext.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        await statusContext.HttpContext.Response.WriteAsJsonAsync(new { message = "Không tìm thấy tài nguyên BFF được yêu cầu." });
    }));
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/spa-api"), branch =>
    branch.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}"));
app.UseStaticFiles();
if (File.Exists(spaIndex))
    app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(Path.GetDirectoryName(spaIndex)!), RequestPath = "/spa" });
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "/";
    if (spaEnabled && File.Exists(spaIndex) && HttpMethods.IsGet(context.Request.Method)
        && !context.Request.Query.ContainsKey("legacy")
        && context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase)
        && (path == "/" || Regex.IsMatch(path, @"^/(Account/(Login|Register|Profile|Security|Payments(/[0-9]+)?|Favorites|Notices)|Tours(/Details/[0-9]+)?|Destinations(/Details/[0-9]+)?|Planner|Trips)/?$", RegexOptions.IgnoreCase)))
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(spaIndex);
        return;
    }
    await next();
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
