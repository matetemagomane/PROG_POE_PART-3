using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models
{
    public class UserProfile
    {
        public int UserId { get; set; }

        [Required, Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }

        [Display(Name = "Organisation Name")]
        public string? OrganizationName { get; set; }

        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }
    }
}
