using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models
{
    public class Event
    {
        public int EventId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required, Display(Name = "Event Date")]
        public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30).AddHours(7);

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required, Display(Name = "Event Type")]
        public string EventType { get; set; } = "Run"; // Run, Walk, Cycle

        public string? BannerUrl { get; set; }

        // Only filled by the organiser's "my events" list
        public int CategoryCount { get; set; }
        public int EnrolmentCount { get; set; }

        public List<Category> Categories { get; set; } = new();
    }
}
