// 根据 PRICE_LIST.PRODUCT_TYPE 解析出的发卡计划。

namespace Application.Helpers;

public sealed class CardIssuePlan
{
    // 卡结构类型：0 次卡，1 时效卡
    public string CardType { get; init; } = string.Empty;

    // 次卡总次数
    public int? TotalCounts { get; init; }

    // 时效卡有效天数
    public int? ValidDays { get; init; }
}
