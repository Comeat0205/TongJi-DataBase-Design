// 模拟支付成功购卡的请求 DTO，MVP 阶段用，后面 H 模块接真实订单后可以换掉。

namespace Application.DTOs;

public sealed class PurchaseMembershipCardRequestDto
{
    // 购买人会员编号
    public int MemberId { get; init; }

    // 购买的价格表商品编号
    public int PriceId { get; init; }
}
