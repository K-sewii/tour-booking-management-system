using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.RegularExpressions;
using DongNamTrip.Client;
using DongNamTrip.Contracts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace DongNamTrip.Web.Controllers;

[Route("spa-api")]
public sealed class SpaBridgeController(TripApiClient api, IHttpClientFactory clients, IAntiforgery antiforgery,
    IDataProtectionProvider protection) : Controller
{
    private readonly ITimeLimitedDataProtector profileProof = protection.CreateProtector("SCR21.ProfileLoaded").ToTimeLimitedDataProtector();
    private string? Token => User.FindFirst("api_token")?.Value;
    private static readonly Regex AllowedProxy = new(
        @"^(tours|destinations|itineraries|auth/(password|sessions(/[A-Fa-f0-9]{24})?)|account/(payments(/[0-9]+)?|payment-choice|collections|favorites/(tour|destination)/[0-9]+|activity(/[0-9]+)?|notices(/[0-9]+/read)?|preferences))$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [HttpGet("csrf")]
    public IActionResult Csrf() => Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [HttpGet("session")]
    public async Task<IActionResult> Session()
    {
        if (Token is null) return Ok(new { user = (AuthenticatedUser?)null });
        try { return Ok(new { user = await api.MeAsync(Token) }); }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized)
        {
            await SignOutCookie();
            return Ok(new { user = (AuthenticatedUser?)null });
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            return StatusCode(503, new { message = "Chưa kết nối được API. Vui lòng thử lại." });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest? input)
    {
        if (input is null) return BadRequest(new { message = "Vui lòng nhập thông tin đăng nhập." });
        input.Email = input.Email?.Trim() ?? "";
        if (!Valid(input, out var errors)) return BadRequest(new { errors });
        try
        {
            var session = await api.LoginAsync(input);
            await SignIn(session.User, session.Token);
            return Ok(new { user = session.User });
        }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized)
        {
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            return StatusCode(503, new { message = "Chưa kết nối được API. Vui lòng thử lại." });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest? input)
    {
        if (input is null) return BadRequest(new { message = "Vui lòng nhập thông tin đăng ký." });
        input.Name = input.Name?.Trim() ?? "";
        input.Email = input.Email?.Trim() ?? "";
        input.PhoneNumber = string.IsNullOrWhiteSpace(input.PhoneNumber) ? null : input.PhoneNumber.Trim();
        if (!Valid(input, out var errors)) return BadRequest(new { errors });
        try
        {
            var session = await api.RegisterAsync(input);
            await SignIn(session.User, session.Token);
            return Ok(new { user = session.User });
        }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Conflict)
        {
            return Conflict(new { errors = new { Email = new[] { "Email đã được sử dụng." } } });
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            return StatusCode(503, new { message = "Chưa kết nối được API. Vui lòng thử lại." });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try { if (Token is not null) await api.LogoutAsync(Token); }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException) { }
        await SignOutCookie();
        return NoContent();
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        if (Token is null) return Unauthorized(new { message = "Vui lòng đăng nhập." });
        try
        {
            var profile = await api.ProfileAsync(Token);
            return Ok(new { profile, loadToken = profileProof.Protect(profile.Id.ToString(), TimeSpan.FromHours(4)) });
        }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized) { return await Expired(); }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            return StatusCode(503, new { message = "Chưa tải được hồ sơ. Vui lòng thử lại." });
        }
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] SpaProfileUpdate? request)
    {
        if (Token is null) return Unauthorized(new { message = "Vui lòng đăng nhập." });
        if (request?.Profile is null || string.IsNullOrWhiteSpace(request.LoadToken))
            return BadRequest(new { message = "Hãy tải lại hồ sơ trước khi lưu." });
        try
        {
            if (profileProof.Unprotect(request.LoadToken, out _) != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return BadRequest(new { message = "Hãy tải lại hồ sơ trước khi lưu." });
            var current = await api.ProfileAsync(Token);
            request.Profile.Normalize();
            var validation = request.Profile.ValidateForExistingPhone(current.PhoneNumber);
            if (validation.Count > 0) return BadRequest(new { errors = ValidationErrors(validation) });
            var updated = await api.UpdateProfileAsync(request.Profile, Token);
            await SignIn(new AuthenticatedUser(updated.Id, updated.Name, updated.Email,
                User.FindAll(ClaimTypes.Role).Select(item => item.Value).ToArray()), Token);
            return Ok(updated);
        }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized) { return await Expired(); }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Conflict)
        {
            return Conflict(new { errors = new { Email = new[] { "Email đã được sử dụng." } } });
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return BadRequest(new { message = "Hãy tải lại hồ sơ trước khi lưu." });
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            return StatusCode(503, new { message = "Chưa lưu được hồ sơ. Vui lòng thử lại." });
        }
    }

    [AcceptVerbs("GET", "POST", "PUT", "DELETE")]
    [Route("proxy/{**path}")]
    public async Task<IActionResult> Proxy(string? path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path) || !AllowedProxy.IsMatch(path))
            return NotFound(new { message = "Không tìm thấy tài nguyên BFF được yêu cầu." });
        var isPublic = Request.Method == "GET" && (path is "tours" or "destinations");
        if (path is "tours" or "destinations" && !isPublic) return NotFound();
        if (!isPublic && Token is null) return Unauthorized(new { message = "Vui lòng đăng nhập." });
        using var request = new HttpRequestMessage(new HttpMethod(Request.Method), "api/" + path + Request.QueryString);
        if (!isPublic) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        if (Request.Method is "POST" or "PUT")
        {
            request.Content = new StreamContent(Request.Body);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.TryParse(Request.ContentType, out var contentType)
                ? contentType
                : new MediaTypeHeaderValue("application/json");
        }
        try
        {
            using var response = await clients.CreateClient("spa-proxy").SendAsync(request, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return await Expired();
            if (path == "auth/password" && response.IsSuccessStatusCode) await SignOutCookie();
            Response.StatusCode = (int)response.StatusCode;
            Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
            await response.Content.CopyToAsync(Response.Body, ct);
            return new EmptyResult();
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            return StatusCode(503, new { message = "Chưa kết nối được API. Vui lòng thử lại." });
        }
    }

    private async Task<IActionResult> Expired()
    {
        await SignOutCookie();
        return Unauthorized(new { message = "Phiên đã hết hạn. Vui lòng đăng nhập lại." });
    }

    private Task SignOutCookie() => HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    private Task SignIn(AuthenticatedUser user, string token)
    {
        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email), new("api_token", token)
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
    }

    private static bool Valid(object input, out Dictionary<string, string[]> errors)
    {
        var validation = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(input, new ValidationContext(input), validation, true);
        errors = ValidationErrors(validation);
        return valid;
    }

    private static Dictionary<string, string[]> ValidationErrors(IEnumerable<ValidationResult> validation) =>
        validation.SelectMany(issue => issue.MemberNames.DefaultIfEmpty(string.Empty).Select(member => (member, issue.ErrorMessage)))
            .GroupBy(item => item.member)
            .ToDictionary(group => group.Key, group => group.Select(item => item.ErrorMessage ?? "Dữ liệu chưa hợp lệ.").ToArray());
}

public sealed record SpaProfileUpdate(UpdateProfileRequest Profile, string LoadToken);
