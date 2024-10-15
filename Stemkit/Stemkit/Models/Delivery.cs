using System;
using System.Collections.Generic;

namespace Stemkit.Models;

public partial class Delivery
{
    public int DeliveryId { get; set; }

    public int? OrderId { get; set; }

    public int? StaffId { get; set; }

    public string DeliveryStatus { get; set; } = null!;

    public DateTime? DeliveryDate { get; set; }

    public virtual Order? Order { get; set; }

    public virtual Staff? Staff { get; set; }
}
