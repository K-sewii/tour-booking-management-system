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
    public async Task<IActionResult> Index(int? tourId, int? destinationId, int? itineraryId)
    {
        Itinerary? existing = null;
        if (itineraryId is not null)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Challenge();
            try { existing = await api.ItineraryAsync(itineraryId.Value, User.FindFirst("api_token")!.Value); }
            catch (HttpRequestException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound) { return NotFound(); }
        }
        var catalog = await api.PlannerCatalogAsync();
        var stops = existing?.Stops;
        if (stops is null && (tourId is not null || destinationId is not null))
        {
            // The public tour catalog is still DemoStore; resolve its place names to SQL keys
            // before copying a template into a persistent itinerary.
            var publicPlaces = await api.DestinationsAsync();
            var publicIds = tourId is not null
                ? (await api.ToursAsync()).FirstOrDefault(x => x.Id == tourId)?.DestinationIds ?? []
                : [destinationId!.Value];
            stops = publicIds.Select((id, index) =>
            {
                var name = publicPlaces.FirstOrDefault(x => x.Id == id)?.Name;
                var sqlPlace = catalog.Destinations.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase));
                return sqlPlace is null ? null : new ItineraryStopDraft { DestinationId = sqlPlace.Id, Day = 1, Order = index + 1 };
            }).Where(x => x is not null).Select(x => x!).ToArray();
        }
        stops ??= [];
        return View(new PlannerVm(catalog, stops, existing));
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Save(ItineraryDraft input, int? itineraryId)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Nhập tên, ngày đi, số khách và chọn từ 1 đến 12 địa điểm." });
        try
        {
            var token = User.FindFirst("api_token")!.Value;
            var result = itineraryId is null ? await api.SaveItineraryAsync(input, token) : await api.UpdateItineraryAsync(itineraryId.Value, input, token);
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
