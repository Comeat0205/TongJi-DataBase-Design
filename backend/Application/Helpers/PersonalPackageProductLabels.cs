// 私教课包商品编码：PT_PACKAGE_{courseId}_{sessions}_{days}

using Domain.Exceptions;

namespace Application.Helpers;

public static class PersonalPackageProductLabels
{
    public const string Prefix = "PT_PACKAGE_";
    public const string InactivePrefix = "INACTIVE_";

    public static bool IsPersonalPackageProduct(string? productType)
    {
        if (string.IsNullOrWhiteSpace(productType))
        {
            return false;
        }

        var normalized = NormalizeProductType(productType);
        return normalized.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsActiveProductType(string productType)
        => !productType.StartsWith(InactivePrefix, StringComparison.OrdinalIgnoreCase);

    public static string NormalizeProductType(string productType)
    {
        if (productType.StartsWith(InactivePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return productType[InactivePrefix.Length..];
        }

        return productType;
    }

    public static PersonalPackageIssuePlan FromProductType(string productType)
    {
        var normalized = NormalizeProductType(productType);
        if (!normalized.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("无法识别的私教课包商品类型：" + productType);
        }

        var parts = normalized[Prefix.Length..].Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3
            || !int.TryParse(parts[0], out var courseId)
            || !short.TryParse(parts[1], out var sessions)
            || !int.TryParse(parts[2], out var days)
            || courseId <= 0
            || sessions <= 0
            || days <= 0)
        {
            throw new DomainException(
                "私教课包商品编码格式应为 PT_PACKAGE_{课程编号}_{次数}_{有效天数}，当前为：" + productType);
        }

        return new PersonalPackageIssuePlan
        {
            PersonalCourseId = courseId,
            TotalSessions = sessions,
            ValidDays = days,
        };
    }

    public static string BuildProductType(int personalCourseId, short totalSessions, int validDays)
        => $"{Prefix}{personalCourseId}_{totalSessions}_{validDays}";
}

public sealed class PersonalPackageIssuePlan
{
    public int PersonalCourseId { get; init; }
    public short TotalSessions { get; init; }
    public int ValidDays { get; init; }
}
