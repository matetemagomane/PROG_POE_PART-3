using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.DTOs;

namespace RaceDay.API.Tests
{
    public class EventsControllerTests
    {
        private static CreateEventDto Dto(string type = "Run") => new()
        {
            Title = "Sunrise 5km", Description = "Fun run", EventType = type, Location = "Pretoria", EventDate = DateTime.UtcNow.AddDays(10)
        };

        [Fact]
        public async Task CreateEvent_AsOrganiser_Succeeds()
        {
            var ctx = TestHelpers.NewContext();
            var org = TestHelpers.AddUser(ctx, "Organiser", "org@test.com");
            var controller = TestHelpers.WithSession(new EventsController(ctx), org.UserId, "Organiser");

            var result = await controller.CreateEvent(Dto());

            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(1, await ctx.Events.CountAsync());
        }

        [Fact]
        public async Task CreateEvent_AsParticipant_ReturnsForbidden()
        {
            var ctx = TestHelpers.NewContext();
            var user = TestHelpers.AddUser(ctx, "Participant", "p@test.com");
            var controller = TestHelpers.WithSession(new EventsController(ctx), user.UserId, "Participant");

            var result = await controller.CreateEvent(Dto());

            Assert.Equal(403, TestHelpers.StatusOf(result));
            Assert.Equal(0, await ctx.Events.CountAsync());
        }

        [Fact]
        public async Task CreateEvent_NotLoggedIn_ReturnsForbidden()
        {
            var ctx = TestHelpers.NewContext();
            var controller = TestHelpers.WithSession(new EventsController(ctx), null, null);

            var result = await controller.CreateEvent(Dto());
            Assert.Equal(403, TestHelpers.StatusOf(result));
        }

        [Fact]
        public async Task CreateEvent_InvalidType_ReturnsBadRequest()
        {
            var ctx = TestHelpers.NewContext();
            var org = TestHelpers.AddUser(ctx, "Organiser", "org@test.com");
            var controller = TestHelpers.WithSession(new EventsController(ctx), org.UserId, "Organiser");

            var result = await controller.CreateEvent(Dto("Swim"));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateEvent_OwnEvent_Succeeds()
        {
            var ctx = TestHelpers.NewContext();
            var org = TestHelpers.AddUser(ctx, "Organiser", "org@test.com");
            var ev = TestHelpers.AddEvent(ctx, org.UserId);
            var controller = TestHelpers.WithSession(new EventsController(ctx), org.UserId, "Organiser");

            var dto = Dto("Cycle");
            dto.Title = "Renamed";
            var result = await controller.UpdateEvent(ev.EventId, dto);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Renamed", (await ctx.Events.FindAsync(ev.EventId))!.Title);
        }

        [Fact]
        public async Task UpdateEvent_OtherOrganisersEvent_ReturnsForbidden()
        {
            var ctx = TestHelpers.NewContext();
            var owner = TestHelpers.AddUser(ctx, "Organiser", "owner@test.com");
            var other = TestHelpers.AddUser(ctx, "Organiser", "other@test.com");
            var ev = TestHelpers.AddEvent(ctx, owner.UserId);
            var controller = TestHelpers.WithSession(new EventsController(ctx), other.UserId, "Organiser");

            var result = await controller.UpdateEvent(ev.EventId, Dto());
            Assert.Equal(403, TestHelpers.StatusOf(result));
        }

        [Fact]
        public async Task DeleteEvent_AsOrganiser_RemovesEvent()
        {
            var ctx = TestHelpers.NewContext();
            var org = TestHelpers.AddUser(ctx, "Organiser", "org@test.com");
            var ev = TestHelpers.AddEvent(ctx, org.UserId);
            var controller = TestHelpers.WithSession(new EventsController(ctx), org.UserId, "Organiser");

            var result = await controller.DeleteEvent(ev.EventId);

            Assert.IsType<NoContentResult>(result);
            Assert.Equal(0, await ctx.Events.CountAsync());
        }

        [Fact]
        public async Task DeleteEvent_AsParticipant_ReturnsForbidden()
        {
            var ctx = TestHelpers.NewContext();
            var org = TestHelpers.AddUser(ctx, "Organiser", "org@test.com");
            var user = TestHelpers.AddUser(ctx, "Participant", "p@test.com");
            var ev = TestHelpers.AddEvent(ctx, org.UserId);
            var controller = TestHelpers.WithSession(new EventsController(ctx), user.UserId, "Participant");

            var result = await controller.DeleteEvent(ev.EventId);

            Assert.Equal(403, TestHelpers.StatusOf(result));
            Assert.Equal(1, await ctx.Events.CountAsync());
        }

        [Fact]
        public async Task GetEvents_ParticipantCanView()
        {
            var ctx = TestHelpers.NewContext();
            var org = TestHelpers.AddUser(ctx, "Organiser", "org@test.com");
            TestHelpers.AddEvent(ctx, org.UserId);
            var controller = TestHelpers.WithSession(new EventsController(ctx), 99, "Participant");

            var result = await controller.GetEvents();
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetEvent_Missing_ReturnsNotFound()
        {
            var ctx = TestHelpers.NewContext();
            var controller = TestHelpers.WithSession(new EventsController(ctx), null, null);

            var result = await controller.GetEvent(12345);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetMyEvents_AsParticipant_ReturnsForbidden()
        {
            var ctx = TestHelpers.NewContext();
            var controller = TestHelpers.WithSession(new EventsController(ctx), 5, "Participant");

            var result = await controller.GetMyEvents();
            Assert.Equal(403, TestHelpers.StatusOf(result));
        }
    }
}
