using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly RaceDayApiService _api;

        public AccountController(RaceDayApiService api)
        {
            _api = api;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (HttpContext.Session.GetInt32("UserId") != null) return RedirectToAction("Index", "Home");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var result = await _api.LoginAsync(model);
            if (!result.Success || result.Data == null)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Invalid login attempt.");
                return View(model);
            }

            // Session keeps who is logged in and which role they have
            HttpContext.Session.SetInt32("UserId", result.Data.UserId);
            HttpContext.Session.SetString("UserName", result.Data.FullName);
            HttpContext.Session.SetString("UserEmail", model.Email);
            HttpContext.Session.SetString("UserRole", result.Data.Role);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);

            return result.Data.Role == "Organiser"
                ? RedirectToAction("Dashboard", "Organiser")
                : RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _api.RegisterAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed.");
                return View(model);
            }

            TempData["Success"] = "Registration successful! Please log in.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _api.LogoutAsync();
            HttpContext.Session.Clear();   // logout clears the session
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied() => View();

        // ----- Profile (both roles) -----

        [RoleRequired]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var result = await _api.GetProfileAsync();
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Error ?? "Could not load your profile.";
                return RedirectToAction("Index", "Home");
            }
            return View(result.Data);
        }

        [RoleRequired]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UserProfile model, IFormFile? pictureFile)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _api.UpdateProfileAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not update your profile.");
                return View(model);
            }

            HttpContext.Session.SetString("UserName", model.FullName);
            HttpContext.Session.SetString("UserEmail", model.Email);

            if (pictureFile != null && pictureFile.Length > 0)
            {
                var upload = await _api.UploadProfilePictureAsync(pictureFile);
                if (!upload.Success) TempData["Error"] = upload.Error;
            }

            TempData["Success"] = "Profile updated.";
            return RedirectToAction(nameof(Profile));
        }
    }
}
