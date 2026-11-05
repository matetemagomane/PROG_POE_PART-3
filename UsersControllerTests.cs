using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.DTOs;

namespace RaceDay.API.Tests
{
    public class UsersControllerTests
    {
        [Fact]
        public async Task GetProfile_NotLoggedIn_ReturnsUnauthorized()
        {
            var ctx = TestHelpers.NewContext();
            var controller = TestHelpers.WithSession(new UsersController(ctx), null, null);
            Assert.IsType<UnauthorizedObjectResult>(await controller.GetProfile());
        }

        [Fact]
        public async Task UpdateProfile_ChangesOwnDetails()
        {
            var ctx = TestHelpers.NewContext();
            var user = TestHelpers.AddUser(ctx, "Participant", "p@test.com", "Old Name");
            var controller = TestHelpers.WithSession(new UsersController(ctx), user.UserId, "Participant");

            var result = await controller.UpdateProfile(new UpdateProfileDto { FullName = "New Name", Email = "p@test.com" });

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("New Name", (await ctx.Users.FindAsync(user.UserId))!.FullName);
        }

        [Fact]
        public async Task UpdateProfile_EmailUsedByAnotherUser_ReturnsBadRequest()
        {
            var ctx = TestHelpers.NewContext();
            TestHelpers.AddUser(ctx, "Participant", "taken@test.com");
            var me = TestHelpers.AddUser(ctx, "Participant", "me@test.com");
            var controller = TestHelpers.WithSession(new UsersController(ctx), me.UserId, "Participant");

            var result = await controller.UpdateProfile(new UpdateProfileDto { FullName = "Me", Email = "taken@test.com" });
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
