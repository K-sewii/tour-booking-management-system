using System.Security.Claims;
using DongNamTrip.Client;
using DongNamTrip.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace DongNamTrip.Web.Controllers;

public class AccountController(TripApiClient api) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        SetReturnUrl(returnUrl);
        return View(new LoginRequest());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest input, string? returnUrl)
    {
        NormalizeAndValidate(input);
        SetReturnUrl(returnUrl);
        if (ModelState.IsValid)
        {
            try
            {
                await SignInAsync(await api.LoginAsync(input));
                return RedirectAfterAuthentication(returnUrl);
            }
            catch (HttpRequestException exception)
            {
                ModelState.AddModelError("", exception.StatusCode is null
                    ? "Chưa kết nối được API. Hãy khởi động DongNamTrip.Api ở cổng 5080."
                    : exception.Message);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError("", "API phản hồi chậm. Vui lòng thử lại.");
            }
        }

        ClearLoginPassword(input);
        return View(input);
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl)
    {
        SetReturnUrl(returnUrl);
        return View(new RegisterRequest());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterRequest input, string? returnUrl)
    {
        NormalizeAndValidate(input);
        SetReturnUrl(returnUrl);
        if (ModelState.IsValid)
        {
            try
            {
                await SignInAsync(await api.RegisterAsync(input));
                return RedirectAfterAuthentication(returnUrl);
            }
            catch (HttpRequestException exception)
            {
                ModelState.AddModelError("", exception.StatusCode is null
                    ? "Chưa kết nối được API. Hãy khởi động DongNamTrip.Api ở cổng 5080."
                    : exception.Message);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError("", "API phản hồi chậm. Vui lòng thử lại.");
            }
        }

        ClearRegistrationPasswords(input);
        return View(input);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        try
        {
            if (User.FindFirst("api_token") is { } token)
                await api.LogoutAsync(token.Value);
        }
        catch (HttpRequestException)
        {
        }
        catch (TaskCanceledException)
        {
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        try
        {
            var profile = await api.ProfileAsync(ApiToken());
            return View(ToRequest(profile));
        }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login), new { returnUrl = Url.Action(nameof(Profile)) });
        }
        catch (HttpRequestException exception)
        {
            TempData["ProfileError"] = exception.StatusCode is null
                ? "Chưa kết nối được API. Vui lòng thử lại sau."
                : exception.Message;
            return View(ToRequestFromClaims());
        }
        catch (TaskCanceledException)
        {
            TempData["ProfileError"] = "API phản hồi chậm. Vui lòng thử lại.";
            return View(ToRequestFromClaims());
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Profile(UpdateProfileRequest input)
    {
        if (!ModelState.IsValid)
            return View(input);

        try
        {
            var profile = await api.UpdateProfileAsync(input, ApiToken());
            await SignInAsync(
                new AuthenticatedUser(profile.Id, profile.Name, profile.Email, User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray()),
                ApiToken());
            TempData["ProfileSuccess"] = "Thông tin liên hệ đã được cập nhật.";
            return RedirectToAction(nameof(Profile));
        }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login), new { returnUrl = Url.Action(nameof(Profile)) });
        }
        catch (HttpRequestException exception)
        {
            ModelState.AddModelError("", exception.StatusCode is null
                ? "Chưa kết nối được API. Vui lòng thử lại sau."
                : exception.Message);
        }
        catch (TaskCanceledException)
        {
            ModelState.AddModelError("", "API phản hồi chậm. Vui lòng thử lại.");
        }

        return View(input);
    }

    private Task SignInAsync(LoginResponse session) => SignInAsync(session.User, session.Token);

    private void SetReturnUrl(string? returnUrl) => ViewBag.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;

    private IActionResult RedirectAfterAuthentication(string? returnUrl) => Url.IsLocalUrl(returnUrl)
        ? LocalRedirect(returnUrl!)
        : RedirectToAction("Index", "Home");

    private void NormalizeAndValidate(LoginRequest input)
    {
        input.Email = input.Email?.Trim() ?? "";
        ModelState.Clear();
        TryValidateModel(input);
    }

    private void NormalizeAndValidate(RegisterRequest input)
    {
        input.Name = input.Name?.Trim() ?? "";
        input.Email = input.Email?.Trim() ?? "";
        input.PhoneNumber = string.IsNullOrWhiteSpace(input.PhoneNumber) ? null : input.PhoneNumber.Trim();
        ModelState.Clear();
        TryValidateModel(input);
    }

    private void ClearLoginPassword(LoginRequest input)
    {
        input.Password = "";
        ClearSensitiveModelValue(nameof(LoginRequest.Password));
    }

    private void ClearRegistrationPasswords(RegisterRequest input)
    {
        input.Password = "";
        input.ConfirmPassword = "";
        ClearSensitiveModelValue(nameof(RegisterRequest.Password));
        ClearSensitiveModelValue(nameof(RegisterRequest.ConfirmPassword));
    }

    private void ClearSensitiveModelValue(string key)
    {
        if (!ModelState.TryGetValue(key, out var state))
            return;

        var errors = state.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Dữ liệu chưa hợp lệ." : error.ErrorMessage).ToArray();
        ModelState.Remove(key);
        foreach (var error in errors)
            ModelState.AddModelError(key, error);
    }

    private async Task SignInAsync(AuthenticatedUser user, string token)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new("api_token", token)
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    private string ApiToken() => User.FindFirst("api_token")?.Value
        ?? throw new InvalidOperationException("Phiên Web không chứa token API.");

    private static UpdateProfileRequest ToRequest(AccountProfile profile) => new()
    {
        Name = profile.Name,
        Email = profile.Email,
        PhoneNumber = profile.PhoneNumber
    };

    private UpdateProfileRequest ToRequestFromClaims() => new()
    {
        Name = User.Identity?.Name ?? "",
        Email = User.FindFirstValue(ClaimTypes.Email) ?? ""
    };
}
