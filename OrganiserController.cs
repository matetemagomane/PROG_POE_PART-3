using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [RoleRequired("Organiser")]
    public class OrganiserController : Controller
    {
        private readonly RaceDayApiService _api;

        public OrganiserController(RaceDayApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Dashboard()
        {
            var result = await _api.GetMyEventsAsync();
            if (!result.Success) TempData["Error"] = result.Error;
            return View(result.Data ?? new());
        }
    }
}
