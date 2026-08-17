using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class PaymentDetail
{
    public int DetailId { get; set; }

    public int OrderId { get; set; }

    public int PriceId { get; set; }

    public decimal TransactionPrice { get; set; }

    public int Quantity { get; set; }

    public decimal SubtotalAmount { get; set; }

    public virtual PaymentOrder Order { get; set; } = null!;

    public virtual PriceList Price { get; set; } = null!;
}



