namespace Domain.Interfaces;

public interface IGroupCourseScheduleRepository
{
    /// <summary>
    /// 按周课模式检测：在 [rangeStart, rangeEnd] 内，凡与目标团课相同星期几且时段重叠的教练其他团课，即冲突。
    /// </summary>
    Task<(bool Success, string Message)> CheckWeeklyConflictInRangeAsync(
        int courseId,
        int coachId,
        DateTime rangeStart,
        DateTime rangeEnd,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 按草稿周课模式检测（团课尚未落库或尚未写时间模板时使用）。
    /// excludeCourseId：编辑时排除自身；新建传 0。
    /// weekday：1=周一 … 7=周日。
    /// </summary>
    Task<(bool Success, string Message)> CheckWeeklyConflictWithPatternAsync(
        int excludeCourseId,
        int coachId,
        int weekday,
        TimeSpan startTime,
        TimeSpan endTime,
        DateTime rangeStart,
        DateTime rangeEnd,
        CancellationToken cancellationToken = default);
}
