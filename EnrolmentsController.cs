using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class EnrolmentsController : Controller
    {
        private readonly RaceDayApiService _api;

        public EnrolmentsController(RaceDayApiService api)
        {
            _api = api;
        }

        [RoleRequired("Participant")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Enrol(int categoryId, int eventId)
        {
            var result = await _api.EnrolAsync(categoryId);
            if (!result.Success)
            {
                TempData["Error"] = result.Error ?? "Could not enrol in this category.";
                return RedirectToAction("Details", "Events", new { id = eventId });
            }

            TempData["Success"] = "You are in! Successfully enrolled in the race category.";
            return RedirectToAction(nameof(MyEnrolments));
        }

        [RoleRequired("Participant")]
        public async Task<IActionResult> MyEnrolments()
        {
            var result = await _api.GetMyEnrolmentsAsync();
            if (!result.Success) TempData["Error"] = result.Error;
            return View(result.Data ?? new());
        }

        // Organiser: all enrolments for one of their events (and result entry)
        [RoleRequired("Organiser")]
        public async Task<IActionResult> EventEnrolments(int id)
        {
            var eventResult = await _api.GetEventAsync(id);
            if (!eventResult.Success || eventResult.Data == null)
            {
                TempData["Error"] = eventResult.Error ?? "Event not found.";
                return RedirectToAction("Dashboard", "Organiser");
            }

            var enrolments = await _api.GetEventEnrolmentsAsync(id);
            if (!enrolments.Success)
            {
                TempData["Error"] = enrolments.Error;
                return RedirectToAction("Dashboard", "Organiser");
            }

            ViewBag.Event = eventResult.Data;
            return View(enrolments.Data ?? new());
        }
    }
}
