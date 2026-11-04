using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
using System.Diagnostics;

namespace RaceDay.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly RaceDayApiService _api;

        public HomeController(RaceDayApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            // Organisers land on their dashboard
            if (HttpContext.Session.GetString("UserRole") == "Organiser")
                return RedirectToAction("Dashboard", "Organiser");

            var result = await _api.GetEventsAsync();
            if (!result.Success) ViewBag.ApiError = result.Error;

            var upcoming = (result.Data ?? new()).Where(e => e.EventDate >= DateTime.Now.Date).OrderBy(e => e.EventDate).Take(3).ToList();
            return View(upcoming);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
