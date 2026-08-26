using Domain.Entities;

namespace Application.Services;

internal static class PersonalTrainingRules
{
    private static readonly HashSet<string> InactivePackageStatuses =
    [
        "2",
        "INACTIVE",
        "EXPIRED",
        "CANCELLED",
        "已过期",
        "已取消",
        "停用"
    ];

    public static bool IsPackageUsable(Personalpackage package, DateTime now)
    {
        var status = package.PackageStatus.Trim().ToUpperInvariant();
        return package.RemainingSessions > 0
            && package.ExpireDate.Date >= now.Date
            && !InactivePackageStatuses.Contains(status);
    }

    public static string GetBookingStatus(Ptbooking booking)
    {
        if (booking.MemberConfirmed == "2")
        {
            return "CANCELLED";
        }

        return booking.CoachConfirmed switch
        {
            "1" => "CONFIRMED",
            "2" => "REJECTED",
            _ => "PENDING"
        };
    }
}
