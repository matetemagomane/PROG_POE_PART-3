using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Helpers;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers
{
    /// <summary>Events. Everyone can view; only Organisers can create, update and delete (their own events).</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private static readonly string[] AllowedTypes = { "Run", "Walk", "Cycle" };
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>List all events with their categories (both roles).</summary>
        /// <response code="200">List of events.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Events
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventId,
                    e.Title,
                    e.Description,
                    e.EventType,
                    e.Location,
                    e.EventDate,
                    e.BannerUrl,
                    Categories = e.Categories.Select(c => new { c.CategoryId, c.EventId, c.Name, c.DistanceKm, c.EntryFee, c.MaxCapacity })
                })
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>List the logged-in Organiser's own events, with enrolment counts.</summary>
        /// <response code="200">The organiser's events.</response>
        /// <response code="403">Organiser access required.</response>
        [HttpGet("mine")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyEvents()
        {
            if (!HttpContext.IsOrganiser())
                return StatusCode(403, new { message = "Forbidden: Organiser access required." });

            var userId = HttpContext.GetUserId()!.Value;
            var events = await _context.Events
                .Where(e => e.OrganiserUserId == userId)
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventId,
                    e.Title,
                    e.Description,
                    e.EventType,
                    e.Location,
                    e.EventDate,
                    e.BannerUrl,
                    CategoryCount = e.Categories.Count(),
                    EnrolmentCount = e.Categories.SelectMany(c => c.Enrolments).Count()
                })
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>Get one event by id (both roles).</summary>
        /// <response code="200">The event with its categories.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEvent(int id)
        {
            var ev = await _context.Events
                .Where(e => e.EventId == id)
                .Select(e => new
                {
                    e.EventId,
                    e.Title,
                    e.Description,
                    e.EventType,
                    e.Location,
                    e.EventDate,
                    e.BannerUrl,
                    Categories = e.Categories.Select(c => new { c.CategoryId, c.EventId, c.Name, c.DistanceKm, c.EntryFee, c.MaxCapacity })
                })
                .FirstOrDefaultAsync();

            if (ev == null) return NotFound(new { message = "Event not found." });
            return Ok(ev);
        }

        /// <summary>Create an event (Organiser only).</summary>
        /// <remarks>Needs title, description, date, location and eventType (Run, Walk or Cycle).</remarks>
        /// <response code="201">Event created.</response>
        /// <response code="400">Invalid event type or data.</response>
        /// <response code="403">Organiser access required.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
        {
            if (!HttpContext.IsOrganiser())
                return StatusCode(403, new { message = "Forbidden: Organiser access required." });

            var type = AllowedTypes.FirstOrDefault(t => t.Equals(dto.EventType, StringComparison.OrdinalIgnoreCase));
            if (type == null)
                return BadRequest(new { message = "Event type must be Run, Walk or Cycle." });

            var newEvent = new Event
            {
                OrganiserUserId = HttpContext.GetUserId()!.Value,
                Title = dto.Title,
                Description = dto.Description ?? string.Empty,
                EventType = type,
                Location = dto.Location,
                EventDate = dto.EventDate
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = newEvent.EventId },
                new { newEvent.EventId, newEvent.Title, newEvent.Description, newEvent.EventType, newEvent.Location, newEvent.EventDate });
        }

        /// <summary>Update an event (Organiser only, own events).</summary>
        /// <response code="200">Event updated.</response>
        /// <response code="400">Invalid event type.</response>
        /// <response code="403">Organiser access required, or the event belongs to someone else.</response>
        /// <response code="404">Event not found.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] CreateEventDto dto)
        {
            if (!HttpContext.IsOrganiser())
                return StatusCode(403, new { message = "Forbidden: Organiser access required." });

            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound(new { message = "Event not found." });

            if (ev.OrganiserUserId != HttpContext.GetUserId())
                return StatusCode(403, new { message = "Forbidden: you can only change your own events." });

            var type = AllowedTypes.FirstOrDefault(t => t.Equals(dto.EventType, StringComparison.OrdinalIgnoreCase));
            if (type == null)
                return BadRequest(new { message = "Event type must be Run, Walk or Cycle." });

            ev.Title = dto.Title;
            ev.Description = dto.Description ?? string.Empty;
            ev.EventType = type;
            ev.Location = dto.Location;
            ev.EventDate = dto.EventDate;

            await _context.SaveChangesAsync();
            return Ok(new { ev.EventId, ev.Title, ev.Description, ev.EventType, ev.Location, ev.EventDate });
        }

        /// <summary>Delete an event (Organiser only, own events).</summary>
        /// <response code="204">Event deleted.</response>
        /// <response code="400">The event already has enrolments.</response>
        /// <response code="403">Organiser access required, or the event belongs to someone else.</response>
        /// <response code="404">Event not found.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (!HttpContext.IsOrganiser())
                return StatusCode(403, new { message = "Forbidden: Organiser access required." });

            var ev = await _context.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventId == id);
            if (ev == null) return NotFound(new { message = "Event not found." });

            if (ev.OrganiserUserId != HttpContext.GetUserId())
                return StatusCode(403, new { message = "Forbidden: you can only delete your own events." });

            var categoryIds = ev.Categories.Select(c => c.CategoryId).ToList();
            if (await _context.Enrolments.AnyAsync(en => categoryIds.Contains(en.CategoryId)))
                return BadRequest(new { message = "This event already has enrolments and cannot be deleted." });

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Upload an event banner image to Azure Blob Storage (Organiser only, own events).</summary>
        /// <response code="200">Returns the blob URL that was saved on the event.</response>
        /// <response code="400">No file uploaded.</response>
        /// <response code="403">Organiser access required, or the event belongs to someone else.</response>
        /// <response code="404">Event not found.</response>
        [HttpPost("{id}/upload-banner")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadBanner(int id, IFormFile file, [FromServices] BlobStorageService blobService)
        {
            if (!HttpContext.IsOrganiser())
                return StatusCode(403, new { message = "Organiser access required." });

            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound(new { message = "Event not found." });

            if (ev.OrganiserUserId != HttpContext.GetUserId())
                return StatusCode(403, new { message = "Forbidden: you can only change your own events." });

            if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded." });

            string bannerUrl = await blobService.UploadFileAsync(file);
            ev.BannerUrl = bannerUrl;
            await _context.SaveChangesAsync();

            return Ok(new { bannerUrl });
        }
    }
}
