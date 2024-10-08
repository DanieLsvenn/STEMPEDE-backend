using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stem.Data.DTO
{
    public class UserProfileDTO
    {
        [Required]
        public string? UserName { get; set; }
        [MaxLength(45)]
        public string? FullName { get; set; }
        [MaxLength(45)]
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        private int points = 0;
       
        
    }
}
