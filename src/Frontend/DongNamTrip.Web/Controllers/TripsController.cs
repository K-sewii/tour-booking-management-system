using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using DongNamTrip.Client;
using DongNamTrip.Web.Models;

namespace DongNamTrip.Web.Controllers;
[Authorize]
public class TripsController(TripApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        try
        {
            return View(new TripsVm(await api.ItinerariesAsync(User.FindFirst("api_token")!.Value), await api.DestinationsAsync()));
        }
        catch (HttpRequestException e)when (e.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account", new { returnUrl = "/Trips" });
        }
    }
}
