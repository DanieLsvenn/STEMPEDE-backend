using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stem.Data.DTO
{
    public class UserUpdateDTO
    {
        [Required]
        [MaxLength(45)]
        public string? FullName { get; set; }
        [MaxLength(45)]
        public string? PhoneNumber { get; set; }
        [MaxLength(100)]
        public string? Address { get; set; }
    }
}
