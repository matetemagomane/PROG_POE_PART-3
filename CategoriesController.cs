using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /// <summary>Age / distance categories for an event. Everyone can view; Organisers manage them.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>List the categories of an event (both roles).</summary>
        /// <param name="eventId">The event to list categories for.</param>
        /// <response code="200">List of categories.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories([FromQuery] int eventId)
        {
            var list = await _context.Categories
                .Where(c => c.EventId == eventId)
                .Select(c => new { c.CategoryId, c.EventId, c.Name, c.DistanceKm, c.EntryFee, c.MaxCapacity })
                .ToListAsync();
            return Ok(list);
        }

        /// <summary>Add a category to one of the Organiser's events.</summary>
        /// <remarks>Needs eventId, name (e.g. "Under 20", "10km"), distanceKm, entryFee and maxCapacity.</remarks>
        /// <response code="200">Category created.</response>
        /// <response code="403">Organiser access required, or the event belongs to someone else.</response>
        /// <response code="404">Event not found.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
        {
            if (!HttpContext.IsOrganiser())
                return StatusCode(403, new { message = "Forbidden: Organiser access required." });

            var ev = await _context.Events.FindAsync(dto.EventId);
            if (ev == null) return NotFound(new { message = "Event not found." });

            if (ev.OrganiserUserId != HttpContext.GetUserId())
                return StatusCode(403, new { message = "Forbidden: you can only add categories to your own events." });

            var category = new Category
            {
                EventId = dto.EventId,
                Name = dto.Name,
                DistanceKm = dto.DistanceKm,
                EntryFee = dto.EntryFee,
                MaxCapacity = dto.MaxCapacity
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new { category.CategoryId, category.EventId, category.Name, category.DistanceKm, category.EntryFee, category.MaxCapacity });
        }

        /// <summary>Delete a category (Organiser only, own events, no enrolments yet).</summary>
        /// <response code="204">Category deleted.</response>
        /// <response code="400">The category already has enrolments.</response>
        /// <response code="403">Organiser access required, or the event belongs to someone else.</response>
        /// <response code="404">Category not found.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (!HttpContext.IsOrganiser())
                return StatusCode(403, new { message = "Forbidden: Organiser access required." });

            var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null) return NotFound(new { message = "Category not found." });

            if (category.Event.OrganiserUserId != HttpContext.GetUserId())
                return StatusCode(403, new { message = "Forbidden: you can only change your own events." });

            if (await _context.Enrolments.AnyAsync(e => e.CategoryId == id))
                return BadRequest(new { message = "This category already has enrolments and cannot be deleted." });

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
