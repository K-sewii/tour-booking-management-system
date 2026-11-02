using Microsoft.AspNetCore.Mvc;
using DongNamTrip.Client;
using DongNamTrip.Web.Models;

namespace DongNamTrip.Web.Controllers;
public class HomeController(TripApiClient api) : Controller
{
    public async Task<IActionResult> Index() => View(new HomeVm(await api.ToursAsync(), await api.DestinationsAsync()));
    public IActionResult Error()
    {
        Response.StatusCode = 503;
        return View();
    }

    public IActionResult Status(int code)
    {
        Response.StatusCode = code;
        ViewBag.Code = code;
        return View();
    }
}
