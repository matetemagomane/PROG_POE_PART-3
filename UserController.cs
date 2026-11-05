using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Helpers;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers
{
    /// <summary>View and update the logged-in user's own profile (both roles).</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>Get the logged-in user's profile.</summary>
        /// <response code="200">The profile.</response>
        /// <response code="401">Not logged in.</response>
        [HttpGet("profile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProfile()
        {
            var userId = HttpContext.GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Please log in." });

            var user = await _context.Users
                .Include(u => u.EventOrganiser)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);
            if (user == null) return NotFound();

            return Ok(new
            {
                user.UserId,
                user.FullName,
                user.Email,
                user.Role,
                user.ProfilePictureUrl,
                OrganizationName = user.EventOrganiser?.OrganizationName,
                ContactNumber = user.EventOrganiser?.ContactNumber
            });
        }

        /// <summary>Update the logged-in user's own profile.</summary>
        /// <remarks>Send fullName and email. Organisers can also send organisationName and contactNumber.</remarks>
        /// <response code="200">Profile updated.</response>
        /// <response code="400">Email already used by another account.</response>
        /// <response code="401">Not logged in.</response>
        [HttpPut("profile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = HttpContext.GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Please log in." });

            var user = await _context.Users
                .Include(u => u.EventOrganiser)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);
            if (user == null) return NotFound();

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != user.UserId))
            {
                return BadRequest(new { message = "That email address is already used by another account." });
            }

            user.FullName = dto.FullName;
            user.Email = dto.Email;

            if (user.EventOrganiser != null)
            {
                if (!string.IsNullOrWhiteSpace(dto.OrganizationName)) user.EventOrganiser.OrganizationName = dto.OrganizationName;
                user.EventOrganiser.ContactNumber = dto.ContactNumber ?? user.EventOrganiser.ContactNumber;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profile updated.", user.FullName, user.Email });
        }

        /// <summary>Upload a profile picture (stored in Azure Blob Storage).</summary>
        /// <response code="200">Returns the blob URL that was saved.</response>
        /// <response code="400">No file uploaded.</response>
        /// <response code="401">Not logged in.</response>
        [HttpPost("upload-profile-picture")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file, [FromServices] BlobStorageService blobService)
        {
            var userId = HttpContext.GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Please log in." });

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return NotFound();

            if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded." });

            string imageUrl = await blobService.UploadFileAsync(file);
            user.ProfilePictureUrl = imageUrl;
            await _context.SaveChangesAsync();

            return Ok(new { profilePictureUrl = imageUrl });
        }
    }
}
