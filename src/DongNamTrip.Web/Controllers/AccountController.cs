using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using DongNamTrip.Client;
using DongNamTrip.Contracts;

namespace DongNamTrip.Web.Controllers;
public class AccountController(TripApiClient api) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginRequest());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest input, string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (ModelState.IsValid)
        {
            try
            {
                var result = await api.LoginAsync(input);
                var claims = new ClaimsIdentity([new(ClaimTypes.NameIdentifier, result.User.Id.ToString()), new(ClaimTypes.Name, result.User.Name), new(ClaimTypes.Role, result.User.Role), new("api_token", result.Token)], CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claims));
                return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/Trips");
            }
            catch (HttpRequestException e)
            {
                ModelState.AddModelError("", e.StatusCode is null ? "Chưa kết nối được API. Hãy khởi động DongNamTrip.Api ở cổng 5080." : e.Message);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError("", "API phản hồi chậm. Vui lòng thử lại.");
            }
        }

        return View(input);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        try
        {
            if (User.FindFirst("api_token")is { } token)
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

    public IActionResult Register() => View();
}
