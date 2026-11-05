using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Tests
{
    public static class TestHelpers
    {
        public static ApplicationDbContext NewContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        /// <summary>Attach a fake session to a controller. Pass null user/role to simulate a logged-out request.</summary>
        public static T WithSession<T>(T controller, int? userId, string? role) where T : ControllerBase
        {
            var session = new TestSession();
            if (userId.HasValue) session.SetInt32("UserId", userId.Value);
            if (role != null) session.SetString("UserRole", role);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { Session = session }
            };
            return controller;
        }

        public static User AddUser(ApplicationDbContext ctx, string role, string email, string name = "Test User")
        {
            var user = new User { FullName = name, Email = email, PasswordHash = "hash", Role = role };
            ctx.Users.Add(user);
            ctx.SaveChanges();
            if (role == "Organiser")
            {
                ctx.EventOrganisers.Add(new EventOrganiser { UserId = user.UserId, OrganizationName = "Run Club" });
                ctx.SaveChanges();
            }
            return user;
        }

        public static Event AddEvent(ApplicationDbContext ctx, int organiserId, string title = "City 10km")
        {
            var ev = new Event
            {
                OrganiserUserId = organiserId,
                Title = title,
                Description = "A race",
                EventType = "Run",
                Location = "Pretoria",
                EventDate = DateTime.UtcNow.AddDays(30)
            };
            ctx.Events.Add(ev);
            ctx.SaveChanges();
            return ev;
        }

        public static Category AddCategory(ApplicationDbContext ctx, int eventId, string name = "10km", int capacity = 100)
        {
            var cat = new Category { EventId = eventId, Name = name, DistanceKm = 10, EntryFee = 150, MaxCapacity = capacity };
            ctx.Categories.Add(cat);
            ctx.SaveChanges();
            return cat;
        }

        public static Enrolment AddEnrolment(ApplicationDbContext ctx, int userId, int categoryId)
        {
            var en = new Enrolment { UserId = userId, CategoryId = categoryId };
            ctx.Enrolments.Add(en);
            ctx.SaveChanges();
            return en;
        }

        public static int StatusOf(IActionResult result) => result switch
        {
            ObjectResult o => o.StatusCode ?? 200,
            StatusCodeResult s => s.StatusCode,
            _ => 200
        };
    }
}
