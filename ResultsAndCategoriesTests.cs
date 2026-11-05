using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.DTOs;

namespace RaceDay.API.Tests
{
    public class ResultsAndCategoriesTests
    {
        [Fact]
        public async Task PostResult_AsOrganiser_SavesResult()
        {
            var ctx = TestHelpers.NewContext();
            var org = TestHelpers.AddUser(ctx, "Organiser", "org@test.com");
            var user = TestHelpers.AddUser(ctx, "Participant", "p@test.com");
            var ev = TestHelpers.AddEvent(ctx, org.UserId);
            var cat = TestHelpers.AddCategory(ctx, ev.EventId);
            var en = TestHelpers.AddEnrolment(ctx, user.UserId, cat.CategoryId);
            var controller = TestHelpers.WithSession(new ResultsController(ctx), org.UserId, "Organiser");

            var result = await controller.PostResult(new CreateResultDto { EnrolmentId = en.EnrolmentId, FinishTime = "00:45:30", OverallPosition = 3, CategoryPosition = 1 });

            Assert.IsType<OkObjectResult>(result);
            var saved = await ctx.Results.SingleAsync();
            Assert.Equal(2730, saved.TotalSeconds);
        }

        [Fact]
        public async Task PostResult_AsParticipant_ReturnsForbidden()
        {
            var ctx = TestHelpers.NewContext();
            var user = TestHelpers.AddUser(ctx, "Participant", "p@test.com");
            var controller = TestHelpers.WithSession(new ResultsController(ctx), user.UserId, "Participant");

            var result = await controller.PostResult(new CreateResultDto { EnrolmentId = 1, FinishTime = "00:45:30" });
            Assert.Equal(403, TestHelpers.StatusOf(result));
        }

        [Fact]
        public async Task PostResult_BadFinishTime_ReturnsBadRequest()
        {
            var ctx = TestHelpers.NewContext();
            var org = TestHelpers.AddUser(ctx, "Organiser", "org@test.com");
            var user = TestHelpers.AddUser(ctx, "Participant", "p@test.com");
            var ev = TestHelpers.AddEvent(ctx, org.UserId);
            var cat = TestHelpers.AddCategory(ctx, ev.EventId);
            var en = TestHelpers.AddEnrolment(ctx, user.UserId, cat.CategoryId);
            var controller = TestHelpers.WithSession(new ResultsController(ctx), org.UserId, "Organiser");

            var result = await controller.PostResult(new CreateResultDto { EnrolmentId = en.EnrolmentId, FinishTime = "fast" });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PostResult_Twice_ReturnsBadRequest()
        {
            var ctx = TestHelpers.NewContext();
            var org = TestHelpers.AddUser(ctx, "Organiser", "org@test.com");
            var user = TestHelpers.AddUser(ctx, "Participant", "p@test.com");
            var ev = TestHelpers.AddEvent(ctx, org.UserId);
            var cat = TestHelpers.AddCategory(ctx, ev.EventId);
            var en = TestHelpers.AddEnrolment(ctx, user.UserId, cat.CategoryId);
            var controller = TestHelpers.WithSession(new ResultsController(ctx), org.UserId, "Organiser");
            var dto = new CreateResultDto { EnrolmentId = en.EnrolmentId, FinishTime = "00:50:00" };

            await controller.PostResult(dto);
            var second = await controller.PostResult(dto);

            Assert.IsType<BadRequestObjectResult>(second);
        }

        [Fact]
        public async Task MyResults_ReturnsOnlyOwnResults()
        {
            var ctx = TestHelpers.NewContext();
            var org = TestHelpers.AddUser(ctx, "Organiser", "org@test.com");
            var me = TestHelpers.AddUser(ctx, "Participant", "me@test.com");
            var other = TestHelpers.AddUser(ctx, "Participant", "other@test.com");
            var ev = TestHelpers.AddEvent(ctx, org.UserId);
            var cat = TestHelpers.AddCategory(ctx, ev.EventId);
            var mine = TestHelpers.AddEnrolment(ctx, me.UserId, cat.CategoryId);
            var theirs = TestHelpers.AddEnrolment(ctx, other.UserId, cat.CategoryId);

            var organiser = TestHelpers.WithSession(new ResultsController(ctx), org.UserId, "Organiser");
            await organiser.PostResult(new CreateResultDto { EnrolmentId = mine.EnrolmentId, FinishTime = "00:40:00", OverallPosition = 1, CategoryPosition = 1 });
            await organiser.PostResult(new CreateResultDto { EnrolmentId = theirs.EnrolmentId, FinishTime = "00:42:00", OverallPosition = 2, CategoryPosition = 2 });

            var asMe = TestHelpers.WithSession(new ResultsController(ctx), me.UserId, "Participant");
            var ok = Assert.IsType<OkObjectResult>(await asMe.GetMyResults());
            var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
            Assert.Single(list.Cast<object>());
        }

        [Fact]
        public async Task MyResults_NotLoggedIn_ReturnsUnauthorized()
        {
            var ctx = TestHelpers.NewContext();
            var controller = TestHelpers.WithSession(new ResultsController(ctx), null, null);
            Assert.IsType<UnauthorizedObjectResult>(await controller.GetMyResults());
        }

        [Fact]
        public async Task CreateCategory_AsOrganiser_Succeeds_AsParticipant_Forbidden()
        {
            var ctx = TestHelpers.NewContext();
            var org = TestHelpers.AddUser(ctx, "Organiser", "org@test.com");
            var user = TestHelpers.AddUser(ctx, "Participant", "p@test.com");
            var ev = TestHelpers.AddEvent(ctx, org.UserId);
            var dto = new CreateCategoryDto { EventId = ev.EventId, Name = "Under 20", DistanceKm = 5, EntryFee = 80, MaxCapacity = 50 };

            var asOrganiser = TestHelpers.WithSession(new CategoriesController(ctx), org.UserId, "Organiser");
            Assert.IsType<OkObjectResult>(await asOrganiser.CreateCategory(dto));

            var asParticipant = TestHelpers.WithSession(new CategoriesController(ctx), user.UserId, "Participant");
            Assert.Equal(403, TestHelpers.StatusOf(await asParticipant.CreateCategory(dto)));
            Assert.Equal(1, await ctx.Categories.CountAsync());
        }
    }
}
