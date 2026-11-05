using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class CreateResultDto
    {
        [Required]
        public int EnrolmentId { get; set; }

        [Required, MaxLength(20)]
        public string FinishTime { get; set; } = string.Empty; // e.g. "01:45:30"

        public int TotalSeconds { get; set; }
        public int OverallPosition { get; set; }
        public int CategoryPosition { get; set; }
    }
}