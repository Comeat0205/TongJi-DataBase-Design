using Domain.Entities;

namespace Application.Services;

internal static class PersonalTrainingRules
{
    // 共享库 CHECK：PACKAGE_STATUS IN ('有效','已用完','已过期')
    private static readonly HashSet<string> InactivePackageStatuses =
    [
        "2",
        "INACTIVE",
        "EXPIRED",
        "CANCELLED",
        "已过期",
        "已用完",
        "已取消",
        "停用"
    ];

    public static bool IsPackageUsable(Personalpackage package, DateTime now)
    {
        var status = package.PackageStatus.Trim();
        var normalized = status.ToUpperInvariant();
        return package.RemainingSessions > 0
            && package.ExpireDate.Date >= now.Date
            && !InactivePackageStatuses.Contains(status)
            && !InactivePackageStatuses.Contains(normalized);
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

    public static bool IsConsumed(Ptbooking booking)
    {
        return booking.ConsumeStatus == "1";
    }

    public static bool CanConsume(Ptbooking booking, DateTime now)
    {
        return booking.MemberConfirmed == "1"
            && booking.CoachConfirmed == "1"
            && !IsConsumed(booking)
            && booking.SessionTime <= now;
    }

    public static bool CanUndoConsumption(Ptbooking booking)
    {
        return booking.MemberConfirmed == "1"
            && booking.CoachConfirmed == "1"
            && IsConsumed(booking);
    }
}
