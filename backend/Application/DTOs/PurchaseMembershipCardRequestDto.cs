// 会员购卡下单请求：创建待支付订单，支付成功后再发卡。

namespace Application.DTOs;

public sealed class PurchaseMembershipCardRequestDto
{
    // 购买人会员编号
    public int MemberId { get; init; }

    // 购买的价格表商品编号
    public int PriceId { get; init; }

    // 可选：指定优惠券；不传则自动选最优可用券
    public int? VoucherId { get; init; }
}
