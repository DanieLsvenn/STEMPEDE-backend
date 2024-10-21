using System;
using System.Collections.Generic;

namespace Stemkit.Models;

public partial class Subcategory
{
    public int SubcategoryId { get; set; }

    public string SubcategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
