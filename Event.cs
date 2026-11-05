using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Event
    {
        [Key]
        public int EventId { get; set; }

        [Required]
        public int OrganiserUserId { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string EventType { get; set; } = string.Empty; // Run, Walk, Cycle

        [Required, MaxLength(150)]
        public string Location { get; set; } = string.Empty;

        [Required]
        public DateTime EventDate { get; set; }

        // Navigation Properties
        [ForeignKey("OrganiserUserId")]
        public EventOrganiser Organiser { get; set; } = null!;
        public ICollection<Category> Categories { get; set; } = new List<Category>();

        public string? BannerUrl { get; set; }
    }
}

