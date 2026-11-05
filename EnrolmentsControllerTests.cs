using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.DTOs;

namespace RaceDay.API.Tests
{
    public class EnrolmentsControllerTests
    {
        [Fact]
        public async Task Enrol_AsParticipant_RecordsUserAndCategory()
        {
            var ctx = TestHelpers.NewContext();
            var org = TestHelpers.AddUser(ctx, "Organiser", "org@test.com");
            var user = TestHelpers.AddUser(ctx, "Participant", "p@test.com");
            var ev = TestHelpers.AddEvent(ctx, org.UserId);
            var cat = TestHelpers.AddCategory(ctx, ev.EventId);
            var controller = TestHelpers.WithSession(new EnrolmentsController(ctx), user.UserId, "Participant");

            var result = await controller.Enrol(new CreateEnrolmentDto { CategoryId = cat.CategoryId });

            Assert.IsType<OkObjectResult>(result);
            var saved = await ctx.Enrolments.SingleAsync();
            Assert.Equal(user.UserId, saved.UserId);
            Assert.Equal(cat.CategoryId, saved.CategoryId);
        }

        [Fact]
        public async Task Enrol_AsOrganiser_ReturnsForbidden()
        {
            var ctx = TestHelpers.NewContext();
            var org = TestHelpers.AddUser(ctx, "Organiser", "org@test.com");
            var ev = TestHelpers.AddEvent(ctx, org.UserId);
            var cat = TestHelpers.AddCategory(ctx, ev.EventId);
            var controller = TestHelpers.WithSession(new EnrolmentsController(ctx), org.UserId, "Organiser");

            var result = await controller.Enrol(new CreateEnrolmentDto { CategoryId = cat.CategoryId });

            Assert.Equal(403, TestHelpers.StatusOf(result));
            Assert.Equal(0, await ctx.Enrolments.CountAsync());
        }

        [Fact]
        public async Task Enrol_NotLoggedIn_ReturnsForbidden()
        {
            var ctx = TestHelpers.NewContext();
            var controller = TestHelpers.WithSession(new EnrolmentsController(ctx), null, null);

            var result = await controller.Enrol(new CreateEnrolmentDto { CategoryId = 1 });
            Assert.Equal(403, TestHelpers.StatusOf(result));
        }

        [Fact]
        public async Task Enrol_Twice_ReturnsBadRequest()
        {
            var ctx = TestHelpers.NewContext();
            var org = TestHelpers.AddUser(ctx, "Organiser", "org@test.com");
            var user = TestHelpers.AddUser(ctx, "Participant", "p@test.com");
            var ev = TestHelpers.AddEvent(ctx, org.UserId);
            var cat = TestHelpers.AddCategory(ctx, ev.EventId);
            TestHelpers.AddEnrolment(ctx, user.UserId, cat.CategoryId);
            var controller = TestHelpers.WithSession(new EnrolmentsController(ctx), user.UserId, "Participant");

            var result = await controller.Enrol(new CreateEnrolmentDto { CategoryId = cat.CategoryId });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Enrol_FullCategory_ReturnsBadRequest()
        {
            var ctx = TestHelpers.NewContext();
            var org = TestHelpers.AddUser(ctx, "Organiser", "org@test.com");
            var first = TestHelpers.AddUser(ctx, "Participant", "a@test.com");
            var second = TestHelpers.AddUser(ctx, "Participant", "b@test.com");
            var ev = TestHelpers.AddEvent(ctx, org.UserId);
            var cat = TestHelpers.AddCategory(ctx, ev.EventId, capacity: 1);
            TestHelpers.AddEnrolment(ctx, first.UserId, cat.CategoryId);
            var controller = TestHelpers.WithSession(new EnrolmentsController(ctx), second.UserId, "Participant");

            var result = await controller.Enrol(new CreateEnrolmentDto { CategoryId = cat.CategoryId });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Enrol_UnknownCategory_ReturnsNotFound()
        {
            var ctx = TestHelpers.NewContext();
            var user = TestHelpers.AddUser(ctx, "Participant", "p@test.com");
            var controller = TestHelpers.WithSession(new EnrolmentsController(ctx), user.UserId, "Participant");

            var result = await controller.Enrol(new CreateEnrolmentDto { CategoryId = 999 });
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task MyEnrolments_NotLoggedIn_ReturnsUnauthorized()
        {
            var ctx = TestHelpers.NewContext();
            var controller = TestHelpers.WithSession(new EnrolmentsController(ctx), null, null);

            var result = await controller.GetMyEnrolments();
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task EventEnrolments_OrganiserSeesOwnEvent_ParticipantRejected()
        {
            var ctx = TestHelpers.NewContext();
            var org = TestHelpers.AddUser(ctx, "Organiser", "org@test.com");
            var user = TestHelpers.AddUser(ctx, "Participant", "p@test.com");
            var ev = TestHelpers.AddEvent(ctx, org.UserId);
            var cat = TestHelpers.AddCategory(ctx, ev.EventId);
            TestHelpers.AddEnrolment(ctx, user.UserId, cat.CategoryId);

            var asOrganiser = TestHelpers.WithSession(new EnrolmentsController(ctx), org.UserId, "Organiser");
            Assert.IsType<OkObjectResult>(await asOrganiser.GetEventEnrolments(ev.EventId));

            var asParticipant = TestHelpers.WithSession(new EnrolmentsController(ctx), user.UserId, "Participant");
            Assert.Equal(403, TestHelpers.StatusOf(await asParticipant.GetEventEnrolments(ev.EventId)));
        }
    }
}
