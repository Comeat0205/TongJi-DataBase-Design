namespace Application.DTOs;

public sealed class UpdateOrderVoucherRequestDto
{
    /// <summary>
    /// 指定券 ID；传 null 表示不使用优惠券。
    /// </summary>
    public int? VoucherId { get; set; }
}
