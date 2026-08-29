namespace Application.DTOs;

public sealed class UpdateOrderVoucherRequestDto
{
    /// <summary>
    /// 指定券 ID；传 null 表示不使用优惠券。
    /// </summary>
    public int? VoucherId { get; set; }

    /// <summary>
    /// 订单无券时改券需带会员 ID（PAYMENT_ORDER 不含 MEMBER_ID）。
    /// </summary>
    public int? MemberId { get; set; }
}
