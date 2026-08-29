// 可购买的会员卡商品 DTO，数据主要来自 PRICE_LIST 表。

namespace Application.DTOs;

public sealed class CardProductDto
{
    // 价格表主键，购买和支付都以这个为准
    public int PriceId { get; init; }

    // 价格表里的商品类型编码，例如 MEMBERSHIP_TIME_90
    public string ProductType { get; init; } = string.Empty;

    // 页面上显示的名字，例如"季卡"，由 MembershipCardLabels 从 productType 转换
    public string Name { get; init; } = string.Empty;

    // 卡结构类型：'0'=次卡，'1'=时效卡，由 productType 解析出来
    public string CardType { get; init; } = string.Empty;

    // 标准定价
    public decimal Price { get; init; }

    // 商品说明，可以从 productType 拼出来
    public string? Description { get; init; }
}
