namespace Domain.Constants;

/// <summary>
/// 系统仅支持三种优惠券，面额与有效期规则固定。
/// </summary>
public static class VoucherTypes
{
    public const string Birthday = "生日福利券";
    public const string Welcome = "新客体验券";
    public const string StaffDiscount = "折扣券";

    public const decimal BirthdayAmount = 66m;
    public const decimal WelcomeAmount = 50m;
    public const decimal StaffDiscountAmount = 33m;

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Birthday,
        Welcome,
        StaffDiscount,
    };

    public static bool IsKnown(string? type) =>
        !string.IsNullOrWhiteSpace(type) && All.Contains(type.Trim());
}
