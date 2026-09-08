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

    /// <summary>
    /// 预约已占用次数后，确认/消课只需检查课包未过期且未永久停用。
    /// </summary>
    public static bool IsPackageActiveForHeldBooking(Personalpackage package, DateTime now)
    {
        var status = package.PackageStatus.Trim();
        var normalized = status.ToUpperInvariant();
        return package.ExpireDate.Date >= now.Date
            && status != "已过期"
            && status != "已取消"
            && status != "停用"
            && normalized is not ("EXPIRED" or "CANCELLED" or "INACTIVE" or "2");
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

    /// <summary>
    /// 会员取消：
    /// - 待教练确认：随时可取消；
    /// - 教练已确认：须距上课超过 24 小时；
    /// - 已消课 / 已拒绝 / 已取消：不可取消。
    /// </summary>
    public static bool CanMemberCancel(Ptbooking booking, DateTime now)
    {
        if (booking.MemberConfirmed == "2")
        {
            return false;
        }

        if (booking.CoachConfirmed == "2")
        {
            return false;
        }

        if (IsConsumed(booking))
        {
            return false;
        }

        if (booking.MemberConfirmed != "1")
        {
            return false;
        }

        // 待教练确认：可随时取消
        if (booking.CoachConfirmed == "0")
        {
            return true;
        }

        // 教练已确认：须距上课超过 24 小时
        return booking.SessionTime - now > TimeSpan.FromHours(24);
    }

    public static void RefundSession(Personalpackage package)
    {
        if (package.RemainingSessions < package.TotalSessions)
        {
            package.RemainingSessions++;
        }

        if (package.PackageStatus.Trim() == "已用完" && package.RemainingSessions > 0)
        {
            package.PackageStatus = "有效";
        }
    }
}
