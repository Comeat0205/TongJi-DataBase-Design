using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class PriceList
{
    public int PriceId { get; set; }

    public string ProductType { get; set; } = null!;

    public decimal StandardPrice { get; set; }

    public DateTime? PriceUpdateTime { get; set; }

    public virtual ICollection<PaymentDetail> PaymentDetails { get; set; } = new List<PaymentDetail>();
}



