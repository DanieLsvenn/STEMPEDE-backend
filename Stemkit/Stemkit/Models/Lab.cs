using System;
using System.Collections.Generic;

namespace Stemkit.Models;

public partial class Lab
{
    public int LabId { get; set; }

    public string? LabName { get; set; }

    public string? Description { get; set; }

    public string? LabFileUrl { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
