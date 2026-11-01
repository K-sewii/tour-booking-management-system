using Microsoft.AspNetCore.Mvc;
using DongNamTrip.Client;
using DongNamTrip.Contracts;
using DongNamTrip.Web.Models;

namespace DongNamTrip.Web.Controllers;
public class ToursController(TripApiClient api) : Controller
{
    public async Task<IActionResult> Index(string? q, string? region, string? category, string? sort, int page = 1)
    {
        if (User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(q))
            try { await api.RecordActivityAsync(new AccountActivityDraft("search", null, q.Trim()), User.FindFirst("api_token")!.Value); }
            catch (Exception error) when (error is HttpRequestException or TaskCanceledException) { }
        var tours = (await api.ToursAsync()).Where(x => (string.IsNullOrWhiteSpace(q) || (x.Name + " " + x.Summary).Contains(q.Trim(), StringComparison.CurrentCultureIgnoreCase)) && (string.IsNullOrEmpty(region) || x.Region == region) && (string.IsNullOrEmpty(category) || x.Category == category));
        tours = sort switch
        {
            "price-asc" => tours.OrderBy(x => x.ReferencePrice),
            "price-desc" => tours.OrderByDescending(x => x.ReferencePrice),
            _ => tours.OrderBy(x => x.Id)};
        var list = tours.ToList();
        var pages = Math.Max(1, (int)Math.Ceiling(list.Count / 4d));
        page = Math.Clamp(page, 1, pages);
        return View(new TourListVm(list.Skip((page - 1) * 4).Take(4).ToList(), q, region, category, sort, page, pages, list.Count));
    }

    public async Task<IActionResult> Details(int id)
    {
        var tour = (await api.ToursAsync()).FirstOrDefault(x => x.Id == id);
        if (tour is null)
            return NotFound();
        var places = await api.DestinationsAsync();
        if (User.Identity?.IsAuthenticated == true)
        {
            try
            {
                var token = User.FindFirst("api_token")!.Value;
                await api.RecordActivityAsync(new AccountActivityDraft("tour", id, null), token);
                ViewBag.IsFavorite = (await api.CollectionsAsync(token)).Favorites.Any(item => item.Kind == "tour" && item.ItemId == id);
            }
            catch (Exception error) when (error is HttpRequestException or TaskCanceledException) { ViewBag.FavoritesUnavailable = true; }
        }
        return View(new TourDetailVm(tour, tour.DestinationIds.Select(key => places.Single(x => x.Id == key)).ToList()));
    }
}
