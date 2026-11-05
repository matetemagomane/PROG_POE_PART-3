using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class EventOrganiser
    {
        [Key, ForeignKey("User")]
        public int UserId { get; set; }

        [Required, MaxLength(150)]
        public string OrganizationName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string ContactNumber { get; set; } = string.Empty;

        // Navigation Properties
        public User User { get; set; } = null!;
        public ICollection<Event> Events { get; set; } = new List<Event>();


    }
}


