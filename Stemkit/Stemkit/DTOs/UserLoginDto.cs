using System.ComponentModel.DataAnnotations;

namespace Stemkit.DTOs
{
    public class UserLoginDto
    {
        [Required]
        public string EmailOrUsername { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
    }

}
