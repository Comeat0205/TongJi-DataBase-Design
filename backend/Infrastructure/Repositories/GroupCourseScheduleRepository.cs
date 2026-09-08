using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class GroupCourseScheduleRepository : IGroupCourseScheduleRepository
{
    private readonly AppDbContext _context;

    public GroupCourseScheduleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message)> CheckWeeklyConflictInRangeAsync(
        int courseId,
        int coachId,
        DateTime rangeStart,
        DateTime rangeEnd,
        CancellationToken cancellationToken = default)
    {
        var start = rangeStart.Date;
        var end = rangeEnd.Date;
        if (end < start)
        {
            return (false, "结束日期不能早于开始日期");
        }

        var course = await _context.Groupcourses
            .AsNoTracking()
            .Include(c => c.TimeSlot)
                .ThenInclude(t => t.TimeSlotInstances)
            .FirstOrDefaultAsync(c => c.CourseId == courseId, cancellationToken);

        if (course is null)
        {
            return (false, "团课不存在");
        }

        var patterns = course.TimeSlot.TimeSlotInstances
            .Select(i => (
                Weekday: i.CourseDate.DayOfWeek,
                Start: i.StartTime.TimeOfDay,
                End: i.EndTime.TimeOfDay))
            .Where(p => p.End > p.Start)
            .Distinct()
            .ToList();

        if (patterns.Count == 0)
        {
            return (false, "该团课尚未配置上课时间模板，无法检测冲突");
        }

        return await EvaluateConflictsAsync(courseId, coachId, start, end, patterns, cancellationToken);
    }

    public async Task<(bool Success, string Message)> CheckWeeklyConflictWithPatternAsync(
        int excludeCourseId,
        int coachId,
        int weekday,
        TimeSpan startTime,
        TimeSpan endTime,
        DateTime rangeStart,
        DateTime rangeEnd,
        CancellationToken cancellationToken = default)
    {
        var start = rangeStart.Date;
        var end = rangeEnd.Date;
        if (end < start)
        {
            return (false, "结束日期不能早于开始日期");
        }

        if (weekday is < 1 or > 7)
        {
            return (false, "上课星期无效");
        }

        if (endTime <= startTime)
        {
            return (false, "结束时间必须晚于开始时间");
        }

        var patterns = new List<(DayOfWeek Weekday, TimeSpan Start, TimeSpan End)>
        {
            (ToDayOfWeek(weekday), startTime, endTime)
        };

        return await EvaluateConflictsAsync(excludeCourseId, coachId, start, end, patterns, cancellationToken);
    }

    private async Task<(bool Success, string Message)> EvaluateConflictsAsync(
        int excludeCourseId,
        int coachId,
        DateTime start,
        DateTime end,
        IReadOnlyList<(DayOfWeek Weekday, TimeSpan Start, TimeSpan End)> patterns,
        CancellationToken cancellationToken)
    {
        var otherSlots = await _context.Groupcourses
            .AsNoTracking()
            .Where(c => c.CoachId == coachId && c.CourseId != excludeCourseId)
            .SelectMany(c => c.TimeSlot.TimeSlotInstances.Select(i => new
            {
                c.CourseId,
                c.CourseName,
                i.CourseDate,
                i.StartTime,
                i.EndTime,
            }))
            .ToListAsync(cancellationToken);

        var conflictDates = new List<string>();
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            foreach (var pattern in patterns.Where(p => p.Weekday == day.DayOfWeek))
            {
                var windowStart = day.Add(pattern.Start);
                var windowEnd = day.Add(pattern.End);

                var hit = otherSlots.FirstOrDefault(o =>
                    o.CourseDate.Date == day
                    && windowStart < o.EndTime
                    && windowEnd > o.StartTime);

                if (hit is not null)
                {
                    conflictDates.Add(
                        $"{day:yyyy-MM-dd}（{GetWeekdayLabel(day.DayOfWeek)}）与「{hit.CourseName}」冲突");
                }
            }
        }

        if (conflictDates.Count > 0)
        {
            var preview = string.Join("；", conflictDates.Take(3));
            var more = conflictDates.Count > 3 ? $"等共 {conflictDates.Count} 处" : string.Empty;
            return (false, $"教练时间冲突：{preview}{more}");
        }

        var weekdays = string.Join("、",
            patterns.Select(p => GetWeekdayLabel(p.Weekday)).Distinct());
        return (true, $"排课无冲突（已按周课模式检查 {start:yyyy-MM-dd}～{end:yyyy-MM-dd}，星期：{weekdays}）");
    }

    private static DayOfWeek ToDayOfWeek(int weekday) => weekday switch
    {
        1 => DayOfWeek.Monday,
        2 => DayOfWeek.Tuesday,
        3 => DayOfWeek.Wednesday,
        4 => DayOfWeek.Thursday,
        5 => DayOfWeek.Friday,
        6 => DayOfWeek.Saturday,
        7 => DayOfWeek.Sunday,
        _ => DayOfWeek.Monday,
    };

    private static string GetWeekdayLabel(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "周一",
        DayOfWeek.Tuesday => "周二",
        DayOfWeek.Wednesday => "周三",
        DayOfWeek.Thursday => "周四",
        DayOfWeek.Friday => "周五",
        DayOfWeek.Saturday => "周六",
        DayOfWeek.Sunday => "周日",
        _ => day.ToString(),
    };
}
