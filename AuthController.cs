using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /// <summary>Registration, login and logout. Login starts a server-side session holding the user's id and role.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>Register a new account as an Organiser or a Participant.</summary>
        /// <remarks>Expects full name, email, password (min 6 chars) and role. Organisers may also send organisationName and contactNumber. The password is stored as a BCrypt hash.</remarks>
        /// <response code="200">Account created.</response>
        /// <response code="400">Email already used, invalid role or invalid data.</response>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest(new { message = "Email address is already registered." });
            }

            if (dto.Role != "Organiser" && dto.Role != "Participant")
            {
                return BadRequest(new { message = "Role must be either 'Organiser' or 'Participant'." });
            }

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (dto.Role == "Organiser")
            {
                _context.EventOrganisers.Add(new EventOrganiser
                {
                    UserId = user.UserId,
                    OrganizationName = dto.OrganizationName ?? dto.FullName,
                    ContactNumber = dto.ContactNumber ?? string.Empty
                });
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Registration successful.", userId = user.UserId, role = user.Role });
        }

        /// <summary>Log in with email and password.</summary>
        /// <remarks>On success a session is created that stores the user's id and role. The session cookie must be sent on later requests.</remarks>
        /// <response code="200">Login successful. Returns userId, fullName and role.</response>
        /// <response code="401">Invalid email or password.</response>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            HttpContext.Session.SetInt32(SessionHelper.UserIdKey, user.UserId);
            HttpContext.Session.SetString(SessionHelper.UserRoleKey, user.Role);

            return Ok(new
            {
                message = "Login successful.",
                userId = user.UserId,
                fullName = user.FullName,
                role = user.Role
            });
        }

        /// <summary>Log out and clear the session.</summary>
        /// <response code="200">Session cleared.</response>
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { message = "Logged out successfully." });
        }
    }
}
