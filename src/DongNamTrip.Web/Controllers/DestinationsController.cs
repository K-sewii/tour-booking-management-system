using Microsoft.AspNetCore.Mvc;
using DongNamTrip.Client;
using DongNamTrip.Contracts;
using DongNamTrip.Web.Models;

namespace DongNamTrip.Web.Controllers;
public class DestinationsController(TripApiClient api) : Controller
{
    public async Task<IActionResult> Index(string? q, string? region, string? category)
    {
        if (User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(q))
            try { await api.RecordActivityAsync(new AccountActivityDraft("search", null, q.Trim()), User.FindFirst("api_token")!.Value); }
            catch (Exception error) when (error is HttpRequestException or TaskCanceledException) { }
        return View(new DestinationListVm((await api.DestinationsAsync()).Where(x => (string.IsNullOrWhiteSpace(q) || x.Name.Contains(q.Trim(), StringComparison.CurrentCultureIgnoreCase)) && (string.IsNullOrEmpty(region) || x.Region == region) && (string.IsNullOrEmpty(category) || x.Category == category)).ToList(), q, region, category));
    }
    public async Task<IActionResult> Details(int id)
    {
        var d = (await api.DestinationsAsync()).FirstOrDefault(x => x.Id == id);
        if (d is null) return NotFound();
        if (User.Identity?.IsAuthenticated == true)
        {
            try
            {
                var token = User.FindFirst("api_token")!.Value;
                await api.RecordActivityAsync(new AccountActivityDraft("destination", id, null), token);
                ViewBag.IsFavorite = (await api.CollectionsAsync(token)).Favorites.Any(item => item.Kind == "destination" && item.ItemId == id);
            }
            catch (Exception error) when (error is HttpRequestException or TaskCanceledException) { ViewBag.FavoritesUnavailable = true; }
        }
        return View(d);
    }
}
