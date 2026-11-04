using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Range(0, 1000), Display(Name = "Distance (km)")]
        public decimal DistanceKm { get; set; }

        [Range(0, 100000), Display(Name = "Entry Fee (R)")]
        public decimal EntryFee { get; set; }

        [Range(0, 100000), Display(Name = "Max Capacity")]
        public int MaxCapacity { get; set; } = 100;
    }
}
