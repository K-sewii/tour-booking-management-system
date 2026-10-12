using Microsoft.AspNetCore.Mvc;
using DongNamTrip.Client;
using DongNamTrip.Web.Models;

namespace DongNamTrip.Web.Controllers;
public class DestinationsController(TripApiClient api) : Controller
{
    public async Task<IActionResult> Index(string? q, string? region, string? category) => View(new DestinationListVm((await api.DestinationsAsync()).Where(x => (string.IsNullOrWhiteSpace(q) || x.Name.Contains(q.Trim(), StringComparison.CurrentCultureIgnoreCase)) && (string.IsNullOrEmpty(region) || x.Region == region) && (string.IsNullOrEmpty(category) || x.Category == category)).ToList(), q, region, category));
    public async Task<IActionResult> Details(int id)
    {
        var d = (await api.DestinationsAsync()).FirstOrDefault(x => x.Id == id);
        return d is null ? NotFound() : View(d);
    }
}
