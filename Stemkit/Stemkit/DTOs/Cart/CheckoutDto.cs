using System.ComponentModel.DataAnnotations;

namespace Stemkit.DTOs.Cart
{
    public class CheckoutDto
    {
        [Required]
        public string PaymentMethodId { get; set; } // e.g., from Stripe

        [Required]
        public ShippingAddressDto ShippingAddress { get; set; }
    }
}
