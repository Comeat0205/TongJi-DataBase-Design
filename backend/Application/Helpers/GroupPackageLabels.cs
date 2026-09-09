// 团课课包商品命名与上下架约定（PRICE_LIST.PRODUCT_TYPE）。
// 格式：GROUP_PKG_T{typeId}_{sessions}，下架加 INACTIVE_ 前缀。

namespace Application.Helpers;

public static class GroupPackageLabels
{
    public const string ActivePrefix = "GROUP_PKG_T";
    public const string InactivePrefix = "INACTIVE_";

    public static bool IsGroupPackageProductType(string productType)
    {
        var normalized = NormalizeProductType(productType);
        return normalized.StartsWith(ActivePrefix, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsActiveProductType(string productType)
    {
        return IsGroupPackageProductType(productType)
            && !productType.StartsWith(InactivePrefix, StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeProductType(string productType)
    {
        if (productType.StartsWith(InactivePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return productType[InactivePrefix.Length..];
        }

        return productType;
    }

    public static string DeactivateProductType(string productType)
    {
        if (productType.StartsWith(InactivePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return productType;
        }

        return InactivePrefix + productType;
    }

    public static string ActivateProductType(string productType) => NormalizeProductType(productType);

    // GROUP_PKG_T{typeId}_{sessions}
    public static bool TryParse(string productType, out int typeId, out int sessions)
    {
        typeId = 0;
        sessions = 0;
        var normalized = NormalizeProductType(productType);
        if (!normalized.StartsWith(ActivePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var rest = normalized[ActivePrefix.Length..];
        var parts = rest.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        return int.TryParse(parts[0], out typeId)
            && int.TryParse(parts[1], out sessions)
            && typeId > 0
            && sessions > 0;
    }

    public static string BuildProductType(int typeId, int sessions)
        => $"{ActivePrefix}{typeId}_{sessions}";

    public static string GetDisplayName(string productType, string? typeName = null)
    {
        if (!TryParse(productType, out var typeId, out var sessions))
        {
            return productType;
        }

        var name = string.IsNullOrWhiteSpace(typeName) ? $"类型{typeId}" : typeName.Trim();
        return $"{name}团课课包·{sessions}次";
    }

    public static string GetPackageStatusLabel(string? status) => status?.Trim() switch
    {
        "1" => "可用",
        "0" => "已用完",
        "2" => "作废",
        _ => "未知",
    };
}
