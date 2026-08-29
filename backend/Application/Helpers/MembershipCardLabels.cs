// 会员卡模块用到的一些小工具方法，后面 AppService 写业务时会调用。

namespace Application.Helpers;

public static class MembershipCardLabels
{
    // 把数据库里的 cardType 转成前端能直接显示的中文
    public static string GetCardTypeLabel(string cardType)
    {
        if (cardType == "0")
        {
            return "次卡";
        }

        if (cardType == "1")
        {
            return "时效卡";
        }

        return "未知类型";
    }

    // 把 cardStatus 转成中文说明
    public static string GetCardStatusLabel(string? cardStatus)
    {
        if (cardStatus == "1")
        {
            return "有效";
        }

        if (cardStatus == "0")
        {
            return "无效";
        }

        if (cardStatus == "2")
        {
            return "停用";
        }

        return "未知状态";
    }

    // 根据 productType 判断这是次卡还是时效卡（发卡时要用的）
    public static string GetCardTypeFromProductType(string productType)
    {
        if (productType.StartsWith("MEMBERSHIP_COUNT", StringComparison.OrdinalIgnoreCase))
        {
            return "0";
        }

        if (productType.StartsWith("MEMBERSHIP_TIME", StringComparison.OrdinalIgnoreCase))
        {
            return "1";
        }

        return string.Empty;
    }

    // 把 productType 转成页面上显示的商品名
    public static string GetProductDisplayName(string productType)
    {
        // 联调阶段新增商品时，只要 PRODUCT_TYPE 按约定命名，下面规则会自动拼名字
        if (productType.StartsWith("MEMBERSHIP_COUNT_", StringComparison.OrdinalIgnoreCase))
        {
            var countPart = productType["MEMBERSHIP_COUNT_".Length..];
            return countPart + "次卡";
        }

        if (productType.StartsWith("MEMBERSHIP_TIME_", StringComparison.OrdinalIgnoreCase))
        {
            var dayPart = productType["MEMBERSHIP_TIME_".Length..];
            // 常见天数可以单独起名，其它天数用通用格式
            if (dayPart.Equals("90", StringComparison.OrdinalIgnoreCase))
            {
                return "季卡";
            }

            if (dayPart.Equals("365", StringComparison.OrdinalIgnoreCase))
            {
                return "年卡";
            }

            return dayPart + "天时效卡";
        }

        // 都不匹配就直接显示编码本身
        return productType;
    }

    // 根据 productType 拼一段简单的商品说明
    public static string GetProductDescription(string productType)
    {
        if (productType.StartsWith("MEMBERSHIP_COUNT_", StringComparison.OrdinalIgnoreCase))
        {
            var countPart = productType["MEMBERSHIP_COUNT_".Length..];
            return countPart + "次入场";
        }

        if (productType.StartsWith("MEMBERSHIP_TIME_", StringComparison.OrdinalIgnoreCase))
        {
            var dayPart = productType["MEMBERSHIP_TIME_".Length..];
            return "自发卡日起 " + dayPart + " 天内有效";
        }

        return productType;
    }
}
