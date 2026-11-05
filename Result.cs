using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Result
    {
        [Key]
        public int ResultId { get; set; }

        [Required]
        public int EnrolmentId { get; set; }

        [Required, MaxLength(20)]
        public string FinishTime { get; set; } = string.Empty; // e.g. "01:45:30"

        public int TotalSeconds { get; set; }

        public int OverallPosition { get; set; }

        public int CategoryPosition { get; set; }

        // Navigation Properties
        [ForeignKey("EnrolmentId")]
        public Enrolment Enrolment { get; set; } = null!;
    }
}