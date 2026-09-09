namespace Application.Time;

/// <summary>业务统一使用北京时间（东八区）墙钟。</summary>
public static class BeijingTime
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    public static DateTime Now
    {
        get
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);
            return DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        }
    }

    public static DateTime Today => Now.Date;

    public static DateTime AsWallClock(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => DateTime.SpecifyKind(
                TimeZoneInfo.ConvertTimeFromUtc(value, Zone),
                DateTimeKind.Unspecified),
            DateTimeKind.Local => DateTime.SpecifyKind(
                TimeZoneInfo.ConvertTime(value, Zone),
                DateTimeKind.Unspecified),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Unspecified),
        };
    }

    /// <summary>
    /// 将「UTC 墙钟」（如 Oracle 在 UTC 主机上的 SYSDATE）转为北京墙钟。
    /// Kind 常被驱动标成 Unspecified，不能走 AsWallClock。
    /// </summary>
    public static DateTime FromUtcWallClock(DateTime value)
    {
        var utc = DateTime.SpecifyKind(
            new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Millisecond),
            DateTimeKind.Utc);
        return AsWallClock(utc);
    }

    private static TimeZoneInfo ResolveZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "China Standard Time" : "Asia/Shanghai");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(
                "Asia/Shanghai",
                TimeSpan.FromHours(8),
                "China Standard Time",
                "China Standard Time");
        }
    }
}
