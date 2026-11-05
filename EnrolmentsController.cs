using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /// <summary>Event enrolments. Participants enter an event by picking a category; Organisers view enrolments for their events.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class EnrolmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EnrolmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>Enter an event by selecting a category (Participant only).</summary>
        /// <remarks>The link between the participant, the event and the chosen category is recorded.</remarks>
        /// <response code="200">Enrolment recorded.</response>
        /// <response code="400">Already enrolled in this category, or the category is full.</response>
        /// <response code="403">Participant access required.</response>
        /// <response code="404">Category not found.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Enrol([FromBody] CreateEnrolmentDto dto)
        {
            if (!HttpContext.IsParticipant())
                return StatusCode(403, new { message = "Forbidden: Participant access required." });

            var userId = HttpContext.GetUserId()!.Value;

            var category = await _context.Categories.FindAsync(dto.CategoryId);
            if (category == null) return NotFound(new { message = "Category not found." });

            bool alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e => e.UserId == userId && e.CategoryId == dto.CategoryId);
            if (alreadyEnrolled)
                return BadRequest(new { message = "You are already enrolled in this race category." });

            if (category.MaxCapacity > 0)
            {
                int taken = await _context.Enrolments.CountAsync(e => e.CategoryId == dto.CategoryId);
                if (taken >= category.MaxCapacity)
                    return BadRequest(new { message = "This category is full." });
            }

            var enrolment = new Enrolment
            {
                UserId = userId,
                CategoryId = dto.CategoryId,
                EnrolledAt = DateTime.UtcNow
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return Ok(new { enrolment.EnrolmentId, enrolment.UserId, enrolment.CategoryId, enrolment.EnrolledAt });
        }

        /// <summary>List the logged-in participant's own enrolments.</summary>
        /// <response code="200">The enrolments (event, category and result if any).</response>
        /// <response code="401">Not logged in.</response>
        [HttpGet("my-enrolments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userId = HttpContext.GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Please log in." });

            var list = await _context.Enrolments
                .Where(e => e.UserId == userId.Value)
                .OrderByDescending(e => e.EnrolledAt)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.EnrolledAt,
                    e.CategoryId,
                    Category = e.Category.Name,
                    EventId = e.Category.EventId,
                    Event = e.Category.Event.Title,
                    EventDate = e.Category.Event.EventDate,
                    ParticipantName = e.User.FullName,
                    ParticipantEmail = e.User.Email,
                    HasResult = e.Result != null,
                    FinishTime = e.Result != null ? e.Result.FinishTime : null,
                    OverallPosition = e.Result != null ? (int?)e.Result.OverallPosition : null,
                    CategoryPosition = e.Result != null ? (int?)e.Result.CategoryPosition : null
                })
                .ToListAsync();

            return Ok(list);
        }

        /// <summary>List all enrolments for one of the Organiser's events.</summary>
        /// <response code="200">The enrolments, including any captured result.</response>
        /// <response code="403">Organiser access required, or the event belongs to someone else.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("event/{eventId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            if (!HttpContext.IsOrganiser())
                return StatusCode(403, new { message = "Forbidden: Organiser access required." });

            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound(new { message = "Event not found." });

            if (ev.OrganiserUserId != HttpContext.GetUserId())
                return StatusCode(403, new { message = "Forbidden: you can only view enrolments for your own events." });

            var list = await _context.Enrolments
                .Where(e => e.Category.EventId == eventId)
                .OrderBy(e => e.Category.Name).ThenBy(e => e.EnrolledAt)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.EnrolledAt,
                    e.CategoryId,
                    Category = e.Category.Name,
                    EventId = e.Category.EventId,
                    Event = e.Category.Event.Title,
                    EventDate = e.Category.Event.EventDate,
                    ParticipantName = e.User.FullName,
                    ParticipantEmail = e.User.Email,
                    HasResult = e.Result != null,
                    FinishTime = e.Result != null ? e.Result.FinishTime : null,
                    OverallPosition = e.Result != null ? (int?)e.Result.OverallPosition : null,
                    CategoryPosition = e.Result != null ? (int?)e.Result.CategoryPosition : null
                })
                .ToListAsync();

            return Ok(list);
        }
    }
}
