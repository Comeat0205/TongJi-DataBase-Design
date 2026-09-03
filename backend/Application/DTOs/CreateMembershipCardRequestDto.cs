// 办卡（发卡）时前端传给后端的请求 DTO。

namespace Application.DTOs;

public sealed class CreateMembershipCardRequestDto
{
    // 要给哪个会员发卡
    public int MemberId { get; init; }

    // 买的是价格表里的哪条商品，后端会根据 PRICE_LIST 决定 card_type 和扩展表
    public int PriceId { get; init; }
}
