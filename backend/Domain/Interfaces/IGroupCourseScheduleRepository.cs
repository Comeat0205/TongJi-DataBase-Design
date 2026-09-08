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
}
