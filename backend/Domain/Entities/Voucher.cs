using System;
using System.Collections.Generic;

namespace Domain.Entities;

/// <summary>
/// 会员福利券记录表
/// </summary>
public partial class Voucher
{
    public int VoucherId { get; set; }

    public int MemberId { get; set; }

    public string VoucherType { get; set; } = null!;

    public decimal DiscountValue { get; set; }

    public DateTime ValidUntil { get; set; }

    /// <summary>
    /// 0-未使用，1-已核销，2-已过期
    /// </summary>
    public string? Status { get; set; }

    public virtual ICollection<PaymentOrder> PaymentOrders { get; set; } = new List<PaymentOrder>();
}



