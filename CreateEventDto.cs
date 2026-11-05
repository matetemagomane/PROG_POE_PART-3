using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class CreateEventDto
    {
        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string EventType { get; set; } = string.Empty; // Run, Walk or Cycle

        [Required, MaxLength(150)]
        public string Location { get; set; } = string.Empty;

        [Required]
        public DateTime EventDate { get; set; }
    }
}