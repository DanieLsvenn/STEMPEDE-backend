using System;
using System.Collections.Generic;

namespace SWP391__StempedeKit.Models;

public partial class OrderDetail
{
    public int OrderDetailId { get; set; }

    public int? OrderId { get; set; }

    public int? ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? ProductDescription { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public int? AdditionalSupportInstances { get; set; }

    public virtual Order? Order { get; set; }

    public virtual Product? Product { get; set; }
}
