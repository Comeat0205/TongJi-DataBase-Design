// 从价格表商品编码解析发卡参数。

using Domain.Exceptions;

namespace Application.Helpers;

public static class CardIssuancePolicy
{
    // 根据 productType 生成发卡计划
    public static CardIssuePlan FromProductType(string productType)
    {
        var cardType = MembershipCardLabels.GetCardTypeFromProductType(productType);
        if (string.IsNullOrEmpty(cardType))
        {
            throw new DomainException("无法识别的会员卡商品类型：" + productType);
        }

        if (cardType == "0")
        {
            return BuildCountCardPlan(productType);
        }

        return BuildTimeCardPlan(productType);
    }

    // 解析次卡次数，例如 MEMBERSHIP_COUNT_20
    private static CardIssuePlan BuildCountCardPlan(string productType)
    {
        const string prefix = "MEMBERSHIP_COUNT_";
        if (!productType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("次卡商品编码格式不正确：" + productType);
        }

        var countText = productType[prefix.Length..];
        if (!int.TryParse(countText, out var totalCounts) || totalCounts <= 0)
        {
            throw new DomainException("次卡次数必须大于 0：" + productType);
        }

        return new CardIssuePlan
        {
            CardType = "0",
            TotalCounts = totalCounts,
        };
    }

    // 解析时效卡天数，例如 MEMBERSHIP_TIME_90
    private static CardIssuePlan BuildTimeCardPlan(string productType)
    {
        const string prefix = "MEMBERSHIP_TIME_";
        if (!productType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("时效卡商品编码格式不正确：" + productType);
        }

        var dayText = productType[prefix.Length..];
        if (!int.TryParse(dayText, out var validDays) || validDays <= 0)
        {
            throw new DomainException("时效卡天数必须大于 0：" + productType);
        }

        return new CardIssuePlan
        {
            CardType = "1",
            ValidDays = validDays,
        };
    }
}
