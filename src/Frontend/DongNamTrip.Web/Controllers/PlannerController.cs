using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using DongNamTrip.Client;
using DongNamTrip.Contracts;
using DongNamTrip.Web.Models;

namespace DongNamTrip.Web.Controllers;
public class PlannerController(TripApiClient api) : Controller
{
    public async Task<IActionResult> Index(int? tourId, int? destinationId)
    {
        var ids = tourId is null ? destinationId is null ? Array.Empty<int>() : [destinationId.Value] : (await api.ToursAsync()).FirstOrDefault(x => x.Id == tourId)?.DestinationIds ?? [];
        return View(new PlannerVm(await api.DestinationsAsync(), ids));
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Save(ItineraryDraft input)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Nhập tên, ngày đi, số khách và chọn từ 1 đến 12 địa điểm." });
        try
        {
            var result = await api.SaveItineraryAsync(input, User.FindFirst("api_token")!.Value);
            return Ok(new { id = result.Id, url = Url.Action("Index", "Trips") });
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return StatusCode((int? )e.StatusCode ?? 503, new { message = e.Message });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(503, new { message = "API phản hồi chậm. Vui lòng thử lại." });
        }
    }
}
