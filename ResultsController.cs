using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /// <summary>Race results. Organisers capture finish times and positions; Participants view their own.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ResultsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ResultsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>Capture a finish time and positions for an enrolment (Organiser only, own events).</summary>
        /// <remarks>finishTime is HH:mm:ss, e.g. 01:45:30. totalSeconds is worked out from it if left as 0.</remarks>
        /// <response code="200">Result saved.</response>
        /// <response code="400">Invalid finish time or the enrolment already has a result.</response>
        /// <response code="403">Organiser access required, or the event belongs to someone else.</response>
        /// <response code="404">Enrolment not found.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PostResult([FromBody] CreateResultDto dto)
        {
            if (!HttpContext.IsOrganiser())
                return StatusCode(403, new { message = "Forbidden: Organiser access required." });

            var enrolment = await _context.Enrolments
                .Include(e => e.Category).ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentId == dto.EnrolmentId);
            if (enrolment == null) return NotFound(new { message = "Enrolment not found." });

            if (enrolment.Category.Event.OrganiserUserId != HttpContext.GetUserId())
                return StatusCode(403, new { message = "Forbidden: you can only capture results for your own events." });

            if (!TimeSpan.TryParse(dto.FinishTime, out var finish) || finish <= TimeSpan.Zero)
                return BadRequest(new { message = "Finish time must look like 01:45:30." });

            if (await _context.Results.AnyAsync(r => r.EnrolmentId == dto.EnrolmentId))
                return BadRequest(new { message = "A result has already been captured for this enrolment." });

            var result = new Result
            {
                EnrolmentId = dto.EnrolmentId,
                FinishTime = dto.FinishTime,
                TotalSeconds = dto.TotalSeconds > 0 ? dto.TotalSeconds : (int)finish.TotalSeconds,
                OverallPosition = dto.OverallPosition,
                CategoryPosition = dto.CategoryPosition
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return Ok(new { result.ResultId, result.EnrolmentId, result.FinishTime, result.TotalSeconds, result.OverallPosition, result.CategoryPosition });
        }

        /// <summary>List the logged-in participant's own results.</summary>
        /// <response code="200">The results with event and category names.</response>
        /// <response code="401">Not logged in.</response>
        [HttpGet("my-results")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyResults()
        {
            var userId = HttpContext.GetUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Please log in." });

            var results = await _context.Results
                .Where(r => r.Enrolment.UserId == userId.Value)
                .Select(r => new
                {
                    r.ResultId,
                    r.EnrolmentId,
                    r.FinishTime,
                    r.TotalSeconds,
                    r.OverallPosition,
                    r.CategoryPosition,
                    EventTitle = r.Enrolment.Category.Event.Title,
                    EventDate = r.Enrolment.Category.Event.EventDate,
                    CategoryName = r.Enrolment.Category.Name
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}
