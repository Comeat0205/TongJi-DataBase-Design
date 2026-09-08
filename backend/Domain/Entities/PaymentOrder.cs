using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class PaymentOrder
{
    public int OrderId { get; set; }

    public int BusinessOrderId { get; set; }

    public decimal TotalAmount { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTime? CreateTime { get; set; }

    public DateTime? PaymentFinishTime { get; set; }

    public int? VoucherId { get; set; }

    public virtual ICollection<PaymentDetail> PaymentDetails { get; set; } = new List<PaymentDetail>();

    public virtual Voucher? Voucher { get; set; }
}



