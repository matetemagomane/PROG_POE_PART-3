using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class ResultsController : Controller
    {
        private readonly RaceDayApiService _api;

        public ResultsController(RaceDayApiService api)
        {
            _api = api;
        }

        [RoleRequired("Participant")]
        public async Task<IActionResult> MyResults()
        {
            var result = await _api.GetMyResultsAsync();
            if (!result.Success) TempData["Error"] = result.Error;
            return View(result.Data ?? new());
        }

        // Organiser captures a finish time and positions for one enrolment
        [RoleRequired("Organiser")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Capture(CaptureResultViewModel model)
        {
            var result = await _api.CaptureResultAsync(model);
            if (result.Success) TempData["Success"] = "Result saved.";
            else TempData["Error"] = result.Error;

            return RedirectToAction("EventEnrolments", "Enrolments", new { id = model.EventId });
        }
    }
}
