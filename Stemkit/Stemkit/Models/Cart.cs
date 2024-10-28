using Stemkit.Constants;

namespace Stemkit.Models;

public partial class Cart
{
    public int CartId { get; set; }

    public int UserId { get; set; }

    public DateOnly CreatedDate { get; set; }

    public string Status { get; set; } = CartStatusConstants.Active;

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual User User { get; set; } = null!;
}
