namespace Application.DTOs;

public sealed class CreatePaymentOrderRequestDto
{
    public int MemberId { get; set; }
    public decimal TotalAmount { get; set; } = 199m;
    public int? VoucherId { get; set; }

    /// <summary>
    /// 可选：关联 PRICE_LIST 商品。购卡等业务下单时应传入真实 PriceId。
    /// </summary>
    public int? PriceId { get; set; }
}
