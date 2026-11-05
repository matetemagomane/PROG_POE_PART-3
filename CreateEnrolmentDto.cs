using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class CreateEnrolmentDto
    {
        [Required]
        public int CategoryId { get; set; }
    }
}