using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class EventsController : Controller
    {
        private readonly RaceDayApiService _api;

        public EventsController(RaceDayApiService api)
        {
            _api = api;
        }

        // GET: /Events  (public)
        public async Task<IActionResult> Index(string? type)
        {
            var result = await _api.GetEventsAsync();
            if (!result.Success) ViewBag.ApiError = result.Error;

            var events = (result.Data ?? new()).OrderBy(e => e.EventDate).ToList();
            if (!string.IsNullOrWhiteSpace(type))
                events = events.Where(e => e.EventType.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();

            ViewBag.Type = type;
            return View(events);
        }

        // GET: /Events/Details/5  (public)
        public async Task<IActionResult> Details(int id)
        {
            var result = await _api.GetEventAsync(id);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Error ?? "Event not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(result.Data);
        }

        // ----- Organiser only -----

        [RoleRequired("Organiser")]
        [HttpGet]
        public IActionResult Create() => View(new Event());

        [RoleRequired("Organiser")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event model, IFormFile? bannerFile)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _api.CreateEventAsync(model);
            if (!result.Success || result.Data == null)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create event.");
                return View(model);
            }

            if (bannerFile != null && bannerFile.Length > 0)
            {
                var upload = await _api.UploadBannerAsync(result.Data.EventId, bannerFile);
                if (!upload.Success) TempData["Error"] = "Event created, but the banner upload failed: " + upload.Error;
            }

            TempData["Success"] = "Event created. Now add race categories.";
            return RedirectToAction(nameof(Categories), new { id = result.Data.EventId });
        }

        [RoleRequired("Organiser")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var result = await _api.GetEventAsync(id);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Error ?? "Event not found.";
                return RedirectToAction("Dashboard", "Organiser");
            }
            return View(result.Data);
        }

        [RoleRequired("Organiser")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Event model, IFormFile? bannerFile)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _api.UpdateEventAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update the event.");
                return View(model);
            }

            if (bannerFile != null && bannerFile.Length > 0)
            {
                var upload = await _api.UploadBannerAsync(model.EventId, bannerFile);
                if (!upload.Success) TempData["Error"] = "Event saved, but the banner upload failed: " + upload.Error;
            }

            TempData["Success"] = "Event updated.";
            return RedirectToAction("Dashboard", "Organiser");
        }

        [RoleRequired("Organiser")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _api.DeleteEventAsync(id);
            if (result.Success) TempData["Success"] = "Event deleted.";
            else TempData["Error"] = result.Error;
            return RedirectToAction("Dashboard", "Organiser");
        }

        // GET: /Events/Categories/5  – manage the categories of an event
        [RoleRequired("Organiser")]
        [HttpGet]
        public async Task<IActionResult> Categories(int id)
        {
            var result = await _api.GetEventAsync(id);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Error ?? "Event not found.";
                return RedirectToAction("Dashboard", "Organiser");
            }
            return View(result.Data);
        }

        [RoleRequired("Organiser")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(Category model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please give the category a name and valid numbers.";
                return RedirectToAction(nameof(Categories), new { id = model.EventId });
            }

            var result = await _api.CreateCategoryAsync(model);
            if (result.Success) TempData["Success"] = "Category added.";
            else TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Categories), new { id = model.EventId });
        }

        [RoleRequired("Organiser")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int categoryId, int eventId)
        {
            var result = await _api.DeleteCategoryAsync(categoryId);
            if (result.Success) TempData["Success"] = "Category deleted.";
            else TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Categories), new { id = eventId });
        }
    }
}
