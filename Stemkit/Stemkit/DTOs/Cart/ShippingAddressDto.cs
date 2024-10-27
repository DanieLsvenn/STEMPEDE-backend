using System.ComponentModel.DataAnnotations;

namespace Stemkit.DTOs.Cart
{
    public class ShippingAddressDto
    {
        [Required]
        public string Street { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string State { get; set; }

        [Required]
        public string PostalCode { get; set; }

        [Required]
        public string Country { get; set; }
    }
}
