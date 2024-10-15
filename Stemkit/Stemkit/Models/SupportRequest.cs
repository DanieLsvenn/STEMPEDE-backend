using System;
using System.Collections.Generic;

namespace Stemkit.Models;

public partial class SupportRequest
{
    public int SupportRequestId { get; set; }

    public int? ProductId { get; set; }

    public int? LabId { get; set; }

    public int? CustomerId { get; set; }

    public int? StaffId { get; set; }

    public DateTime RequestDate { get; set; }

    public string Status { get; set; } = null!;

    public virtual Customer? Customer { get; set; }

    public virtual Lab? Lab { get; set; }

    public virtual Product? Product { get; set; }

    public virtual Staff? Staff { get; set; }
}
