﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace Stem.Data.Models;

public partial class SupportRequest
{
    public int SupportRequestId { get; set; }

    public int? ProductId { get; set; }

    public int? LabId { get; set; }

    public int? CustomerId { get; set; }

    public int? StaffId { get; set; }

    public DateOnly? RequestDate { get; set; }

    public string Status { get; set; }

    public virtual Customer Customer { get; set; }

    public virtual Lab Lab { get; set; }

    public virtual Product Product { get; set; }

    public virtual Staff Staff { get; set; }
}