using System.ComponentModel.DataAnnotations;

namespace Stemkit.DTOs
{
    public class UserRegistrationDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Phone]
        public string Phone { get; set; }

        public string Address { get; set; }

        [Required]
        public string Role { get; set; } = "Customer";

        public bool IsExternal { get; set; }
        public string? ExternalProvider { get; set; }
    }
}
