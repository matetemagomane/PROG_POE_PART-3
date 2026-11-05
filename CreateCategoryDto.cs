using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class CreateCategoryDto
    {
        [Required]
        public int EventId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public decimal DistanceKm { get; set; }
        public decimal EntryFee { get; set; }
        public int MaxCapacity { get; set; }
    }
}