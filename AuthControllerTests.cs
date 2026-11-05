using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Tests
{
    public class AuthControllerTests
    {
        private static AuthController Make(RaceDay.API.Data.ApplicationDbContext ctx) =>
            TestHelpers.WithSession(new AuthController(ctx), null, null);

        [Fact]
        public async Task Register_Participant_CreatesUser()
        {
            var ctx = TestHelpers.NewContext();
            var result = await Make(ctx).Register(new RegisterDto { FullName = "Alice", Email = "alice@test.com", Password = "Pass123!", Role = "Participant" });

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(1, await ctx.Users.CountAsync());
        }

        [Fact]
        public async Task Register_Organiser_CreatesUserAndOrganiserProfile()
        {
            var ctx = TestHelpers.NewContext();
            var result = await Make(ctx).Register(new RegisterDto { FullName = "Bob", Email = "bob@test.com", Password = "Pass123!", Role = "Organiser", OrganizationName = "Run Club" });

            Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(await ctx.EventOrganisers.FirstOrDefaultAsync());
        }

        [Fact]
        public async Task Register_StoresHashedPassword_NotPlainText()
        {
            var ctx = TestHelpers.NewContext();
            await Make(ctx).Register(new RegisterDto { FullName = "Alice", Email = "alice@test.com", Password = "Pass123!", Role = "Participant" });

            var user = await ctx.Users.FirstAsync();
            Assert.NotEqual("Pass123!", user.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("Pass123!", user.PasswordHash));
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsBadRequest()
        {
            var ctx = TestHelpers.NewContext();
            TestHelpers.AddUser(ctx, "Participant", "alice@test.com");

            var result = await Make(ctx).Register(new RegisterDto { FullName = "Alice Two", Email = "alice@test.com", Password = "Pass123!", Role = "Participant" });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_InvalidRole_ReturnsBadRequest()
        {
            var ctx = TestHelpers.NewContext();
            var result = await Make(ctx).Register(new RegisterDto { FullName = "Admin", Email = "admin@test.com", Password = "Pass123!", Role = "SuperAdmin" });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkAndStoresRoleInSession()
        {
            var ctx = TestHelpers.NewContext();
            ctx.Users.Add(new User { FullName = "Alice", Email = "alice@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"), Role = "Participant" });
            await ctx.SaveChangesAsync();

            var controller = Make(ctx);
            var result = await controller.Login(new LoginDto { Email = "alice@test.com", Password = "Password123!" });

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Participant", controller.HttpContext.Session.GetString("UserRole"));
            Assert.NotNull(controller.HttpContext.Session.GetInt32("UserId"));
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsUnauthorized_AndNoSession()
        {
            var ctx = TestHelpers.NewContext();
            ctx.Users.Add(new User { FullName = "Alice", Email = "alice@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("RealPassword"), Role = "Participant" });
            await ctx.SaveChangesAsync();

            var controller = Make(ctx);
            var result = await controller.Login(new LoginDto { Email = "alice@test.com", Password = "WrongPassword" });

            Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Null(controller.HttpContext.Session.GetString("UserRole"));
        }

        [Fact]
        public async Task Login_NonExistentUser_ReturnsUnauthorized()
        {
            var ctx = TestHelpers.NewContext();
            var result = await Make(ctx).Login(new LoginDto { Email = "nobody@test.com", Password = "Password" });
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void Logout_ClearsSession_ReturnsOk()
        {
            var ctx = TestHelpers.NewContext();
            var controller = TestHelpers.WithSession(new AuthController(ctx), 1, "Participant");

            var result = controller.Logout();

            Assert.IsType<OkObjectResult>(result);
            Assert.Null(controller.HttpContext.Session.GetString("UserRole"));
            Assert.Null(controller.HttpContext.Session.GetInt32("UserId"));
        }
    }
}
