using System;
using System.Collections.Generic;

namespace Stemkit.Models;

public partial class Lab
{
    public int LabId { get; set; }

    public string LabName { get; set; } = null!;

    public string? Description { get; set; }

    public string LabFileUrl { get; set; } = null!;

    public int? ProductId { get; set; }

    public virtual Product? Product { get; set; }

    public virtual ICollection<SupportRequest> SupportRequests { get; set; } = new List<SupportRequest>();
}
