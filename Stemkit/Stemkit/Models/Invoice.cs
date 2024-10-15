using System;
using System.Collections.Generic;

namespace Stemkit.Models;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public int? OrderId { get; set; }

    public DateTime InvoiceDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string InvoiceType { get; set; } = null!;

    public string? Note { get; set; }

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual Order? Order { get; set; }

    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();
}
