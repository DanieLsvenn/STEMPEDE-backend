using System;
using System.Collections.Generic;

namespace Stemkit.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public int? SubcategoryId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? Description { get; set; }

    public string ProductType { get; set; } = null!;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public int SupportInstances { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<Lab> Labs { get; set; } = new List<Lab>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Subcategory? Subcategory { get; set; }

    public virtual ICollection<SupportRequest> SupportRequests { get; set; } = new List<SupportRequest>();
}
