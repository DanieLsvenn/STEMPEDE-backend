namespace Stemkit.DTOs.Cart
{
    public class CheckOutViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; }
        public CheckoutRequest CheckoutModel { get; set; }
    }
}
