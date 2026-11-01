using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using DongNamTrip.Client;
using DongNamTrip.Contracts;
using DongNamTrip.Web.Models;

namespace DongNamTrip.Web.Controllers;
[Authorize]
public class TripsController(TripApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        try
        {
            var token = User.FindFirst("api_token")!.Value;
            var catalog = await api.PlannerCatalogAsync();
            return View(new TripsVm(await api.ItinerariesAsync(token), catalog.Destinations,
                await api.PrivateTourRequestsAsync(token), await api.PrivateTourOrdersAsync(token)));
        }
        catch (HttpRequestException e)when (e.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account", new { returnUrl = "/Trips" });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestQuote(int itineraryId, PrivateTourRequestDraft draft)
    {
        if (!ModelState.IsValid) { TempData["TripError"] = "Vui lòng nhập thông tin liên hệ hợp lệ."; return RedirectToAction(nameof(Index)); }
        try
        {
            var request = await api.SubmitPrivateTourRequestAsync(itineraryId, draft, User.FindFirst("api_token")!.Value);
            TempData["TripNotice"] = $"Đã gửi yêu cầu tour riêng #{request.Id}.";
        }
        catch (HttpRequestException e) { TempData["TripError"] = e.Message; }
        return Redirect("/Trips#quotes");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptQuote(int requestId, int quoteId)
    {
        try
        {
            var order = await api.AcceptQuoteAsync(requestId, quoteId, User.FindFirst("api_token")!.Value);
            TempData["TripNotice"] = $"Đã chấp nhận báo giá và tạo đơn #{order.Id}.";
        }
        catch (HttpRequestException e) { TempData["TripError"] = e.Message; }
        return Redirect("/Trips#quotes");
    }
}
