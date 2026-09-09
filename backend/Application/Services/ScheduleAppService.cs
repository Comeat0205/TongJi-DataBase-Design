using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public sealed class ScheduleAppService : IScheduleAppService
{
    private readonly IMemberScheduleRepository _memberScheduleRepository;
    private readonly ICoachScheduleRepository _coachScheduleRepository;
    private readonly IPtBookingRepository _ptBookingRepository;
    private readonly IGroupCourseBookingRepository _groupCourseBookingRepository;
    private readonly IGroupcourseRepository _groupcourseRepository;

    public ScheduleAppService(
        IMemberScheduleRepository memberScheduleRepository,
        ICoachScheduleRepository coachScheduleRepository,
        IPtBookingRepository ptBookingRepository,
        IGroupCourseBookingRepository groupCourseBookingRepository,
        IGroupcourseRepository groupcourseRepository)
    {
        _memberScheduleRepository = memberScheduleRepository;
        _coachScheduleRepository = coachScheduleRepository;
        _ptBookingRepository = ptBookingRepository;
        _groupCourseBookingRepository = groupCourseBookingRepository;
        _groupcourseRepository = groupcourseRepository;
    }

    public async Task<IReadOnlyList<MemberScheduleDto>> GetMemberSchedulesAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var schedules = await _memberScheduleRepository.GetByMemberIdAsync(memberId, cancellationToken);
        // 与私教预约一致：按本地墙钟比较时段状态。
        var now = DateTime.Now;
        // 与教练周课表一致：从本周一起返回（含已结束），前端置灰展示。
        var weekStart = StartOfWeekMonday(now.Date);

        // 已取消的不展示；本周及之后（含已结束）保留。
        var activeSchedules = schedules
            .Where(s => !IsCancelledMemberSchedule(s) && MemberScheduleDay(s) >= weekStart)
            .OrderBy(s => s.ScheduleStart)
            .ToList();

        var ptIds = activeSchedules
            .Where(s => string.Equals(s.ScheduleType, "P", StringComparison.OrdinalIgnoreCase)
                && s.SourceRecordId is > 0)
            .Select(s => s.SourceRecordId!.Value)
            .Distinct()
            .ToList();

        var groupBookingIds = activeSchedules
            .Where(s => string.Equals(s.ScheduleType, "G", StringComparison.OrdinalIgnoreCase)
                && s.SourceRecordId is > 0)
            .Select(s => s.SourceRecordId!.Value)
            .Distinct()
            .ToList();

        var ptDetails = await _ptBookingRepository.GetScheduleDetailsByIdsAsync(ptIds, cancellationToken);
        var groupDetails = await ResolveGroupScheduleDetailsAsync(memberId, groupBookingIds, cancellationToken);

        return activeSchedules.Select(s => MapToDto(s, now, ptDetails, groupDetails)).ToList();
    }

    public async Task<IReadOnlyList<CoachScheduleDto>> GetCoachSchedulesAsync(int coachId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        // 与前端周课表一致：从本周一起展示；已过日期的课仍返回，由前端整列置灰。
        var weekStart = StartOfWeekMonday(now.Date);
        var allSchedules = await _coachScheduleRepository.GetByCoachIdAsync(coachId, cancellationToken);

        // 已取消的不展示；本周及之后（含已结束）的课保留。
        var activeFromTable = allSchedules
            .Where(s => !IsCancelledCoachSchedule(s) && ScheduleDay(s) >= weekStart)
            .ToList();

        // 团操课在 GROUPCOURSE + TIME_SLOT_INSTANCE，通常不写入 COACH_SCHEDULE，查询时合并进来。
        var groupSlots = await BuildGroupCourseCoachSlotsAsync(coachId, now, weekStart, cancellationToken);

        // 若表中已有同课程同刻的 G 记录，避免重复。
        var existingGroupKeys = new HashSet<string>(
            activeFromTable
                .Where(s => string.Equals(s.ScheduleType, "G", StringComparison.OrdinalIgnoreCase))
                .Select(s => SlotKey(s.ScheduleDate, s.ScheduleStart, s.ScheduleEnd, s.SourceRecordId)));

        var mergedGroupSlots = groupSlots
            .Where(g => !existingGroupKeys.Contains(SlotKey(g.ScheduleDate, g.ScheduleStart, g.ScheduleEnd, g.SourceRecordId)))
            .ToList();

        var ptIds = activeFromTable
            .Where(s => string.Equals(s.ScheduleType, "P", StringComparison.OrdinalIgnoreCase)
                && s.SourceRecordId is > 0)
            .Select(s => s.SourceRecordId!.Value)
            .Distinct()
            .ToList();

        var ptDetails = await _ptBookingRepository.GetScheduleDetailsByIdsAsync(ptIds, cancellationToken);

        var groupCourseIds = activeFromTable
            .Where(s => string.Equals(s.ScheduleType, "G", StringComparison.OrdinalIgnoreCase)
                && s.SourceRecordId is > 0)
            .Select(s => s.SourceRecordId!.Value)
            .Concat(mergedGroupSlots.Where(s => s.SourceRecordId is > 0).Select(s => s.SourceRecordId!.Value))
            .Distinct()
            .ToList();

        var groupCourseNames = await LoadGroupCourseNamesAsync(groupCourseIds, cancellationToken);

        var dtoList = new List<CoachScheduleDto>();
        foreach (var s in activeFromTable)
        {
            dtoList.Add(MapToDto(s, isConflict: false, ptDetails, groupCourseNames, now));
        }

        dtoList.AddRange(mergedGroupSlots);

        MarkConflicts(dtoList);

        return dtoList
            .OrderBy(s => s.ScheduleStart)
            .ThenBy(s => s.ScheduleId)
            .ToList();
    }

    private async Task<IReadOnlyList<CoachScheduleDto>> BuildGroupCourseCoachSlotsAsync(
        int coachId,
        DateTime now,
        DateTime weekStart,
        CancellationToken cancellationToken)
    {
        var courses = await _groupcourseRepository.GetAllAsync(cancellationToken);
        var mine = courses.Where(c => c.CoachId == coachId).ToList();
        if (mine.Count == 0)
        {
            return [];
        }

        var result = new List<CoachScheduleDto>();
        foreach (var course in mine)
        {
            var instances = course.TimeSlot?.TimeSlotInstances;
            if (instances is null || instances.Count == 0)
            {
                continue;
            }

            foreach (var slot in instances)
            {
                // 本周之前的不返回；本周内已结束的仍返回，供前端置灰展示。
                if (slot.CourseDate.Date < weekStart)
                {
                    continue;
                }

                result.Add(new CoachScheduleDto
                {
                    // 负数 ID：与 COACH_SCHEDULE 主键区分的虚拟团操日程
                    ScheduleId = BuildVirtualGroupScheduleId(course.CourseId, slot.CourseDate, slot.StartTime),
                    CoachId = coachId,
                    ScheduleStart = slot.StartTime,
                    ScheduleEnd = slot.EndTime,
                    ScheduleDate = slot.CourseDate.Date,
                    ScheduleType = "G",
                    SourceRecordId = course.CourseId,
                    Status = ResolveDisplayStatus(slot.StartTime, slot.EndTime, now, fallback: "正常"),
                    IsConflict = false,
                    MemberId = null,
                    MemberName = null,
                    CourseName = course.CourseName,
                });
            }
        }

        return result;
    }

    private static DateTime StartOfWeekMonday(DateTime date)
    {
        var d = date.Date;
        var diff = d.DayOfWeek == DayOfWeek.Sunday ? -6 : DayOfWeek.Monday - d.DayOfWeek;
        return d.AddDays((int)diff);
    }

    private static DateTime ScheduleDay(CoachSchedule schedule)
        => schedule.ScheduleDate.Date != default
            ? schedule.ScheduleDate.Date
            : schedule.ScheduleStart.Date;

    private static DateTime MemberScheduleDay(MemberSchedule schedule)
        => schedule.ScheduleDate.Date != default
            ? schedule.ScheduleDate.Date
            : schedule.ScheduleStart.Date;

    private async Task<IReadOnlyDictionary<int, string>> LoadGroupCourseNamesAsync(
        IReadOnlyList<int> courseIds,
        CancellationToken cancellationToken)
    {
        if (courseIds.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        var courses = await _groupcourseRepository.GetAllAsync(cancellationToken);
        return courses
            .Where(c => courseIds.Contains(c.CourseId))
            .ToDictionary(c => c.CourseId, c => c.CourseName);
    }

    private static int BuildVirtualGroupScheduleId(int courseId, DateTime courseDate, DateTime startTime)
    {
        var hash = HashCode.Combine(courseId, courseDate.Date, startTime);
        if (hash == int.MinValue)
        {
            return -1;
        }

        var id = -Math.Abs(hash);
        return id == 0 ? -1 : id;
    }

    private static string SlotKey(DateTime date, DateTime start, DateTime end, int? sourceId)
        => $"{date:yyyy-MM-dd}|{start:O}|{end:O}|{sourceId ?? 0}";

    private static void MarkConflicts(List<CoachScheduleDto> schedules)
    {
        var conflictIds = new HashSet<int>();
        for (var i = 0; i < schedules.Count; i++)
        {
            for (var j = i + 1; j < schedules.Count; j++)
            {
                var a = schedules[i];
                var b = schedules[j];
                if (a.ScheduleStart < b.ScheduleEnd && a.ScheduleEnd > b.ScheduleStart)
                {
                    conflictIds.Add(a.ScheduleId);
                    conflictIds.Add(b.ScheduleId);
                }
            }
        }

        if (conflictIds.Count == 0)
        {
            return;
        }

        for (var i = 0; i < schedules.Count; i++)
        {
            if (!conflictIds.Contains(schedules[i].ScheduleId))
            {
                continue;
            }

            var s = schedules[i];
            schedules[i] = new CoachScheduleDto
            {
                ScheduleId = s.ScheduleId,
                CoachId = s.CoachId,
                ScheduleStart = s.ScheduleStart,
                ScheduleEnd = s.ScheduleEnd,
                ScheduleDate = s.ScheduleDate,
                ScheduleType = s.ScheduleType,
                SourceRecordId = s.SourceRecordId,
                Status = s.Status,
                IsConflict = true,
                MemberId = s.MemberId,
                MemberName = s.MemberName,
                CourseName = s.CourseName,
            };
        }
    }

    // 按时段计算展示状态：时段前「待上课」，时段内「上课中」，结束后「已完成」。
    private static MemberScheduleDto MapToDto(
        MemberSchedule schedule,
        DateTime now,
        IReadOnlyDictionary<int, PtScheduleDetail> ptDetails,
        IReadOnlyDictionary<int, (string CourseName, int CoachId, string CoachName)> groupDetails)
    {
        var displayStatus = now < schedule.ScheduleStart
            ? "待上课"
            : now < schedule.ScheduleEnd
                ? "上课中"
                : "已完成";

        var isUpcoming = displayStatus == "待上课"
            && schedule.ScheduleStart <= now.AddHours(2);

        string? courseName = null;
        int? coachId = null;
        string? coachName = null;

        if (schedule.SourceRecordId is int sourceId)
        {
            if (string.Equals(schedule.ScheduleType, "P", StringComparison.OrdinalIgnoreCase)
                && ptDetails.TryGetValue(sourceId, out var pt))
            {
                courseName = pt.CourseName;
                coachId = pt.CoachId;
                coachName = pt.CoachName;
            }
            else if (string.Equals(schedule.ScheduleType, "G", StringComparison.OrdinalIgnoreCase)
                && groupDetails.TryGetValue(sourceId, out var group))
            {
                courseName = group.CourseName;
                coachId = group.CoachId;
                coachName = group.CoachName;
            }
        }

        return new MemberScheduleDto
        {
            ScheduleId = schedule.ScheduleId,
            MemberId = schedule.MemberId,
            ScheduleStart = schedule.ScheduleStart,
            ScheduleDate = schedule.ScheduleDate,
            ScheduleEnd = schedule.ScheduleEnd,
            ScheduleType = schedule.ScheduleType,
            SourceRecordId = schedule.SourceRecordId,
            Status = displayStatus,
            IsUpcoming = isUpcoming,
            CourseName = courseName,
            CoachId = coachId,
            CoachName = coachName
        };
    }

    private static bool IsCancelledMemberSchedule(MemberSchedule schedule)
    {
        var status = schedule.Status?.Trim();
        return string.Equals(status, "2", StringComparison.Ordinal)
            || string.Equals(status, "已取消", StringComparison.Ordinal);
    }

    private async Task<IReadOnlyDictionary<int, (string CourseName, int CoachId, string CoachName)>> ResolveGroupScheduleDetailsAsync(
        int memberId,
        IReadOnlyList<int> bookingIds,
        CancellationToken cancellationToken)
    {
        if (bookingIds.Count == 0)
        {
            return new Dictionary<int, (string, int, string)>();
        }

        var bookings = await _groupCourseBookingRepository.GetByMemberIdAsync(memberId, cancellationToken);
        return bookings
            .Where(b => bookingIds.Contains(b.BookingId) && b.Package?.Course is not null)
            .ToDictionary(
                b => b.BookingId,
                b => (
                    b.Package.Course.CourseName,
                    b.Package.Course.CoachId,
                    b.Package.Course.Coach?.CoachName ?? $"教练 #{b.Package.Course.CoachId}"
                ));
    }

    private static bool IsCancelledCoachSchedule(CoachSchedule schedule)
    {
        var status = schedule.Status?.Trim();
        return string.Equals(status, "已取消", StringComparison.Ordinal)
            || string.Equals(status, "2", StringComparison.Ordinal);
    }

    private static string ResolveDisplayStatus(DateTime start, DateTime end, DateTime now, string fallback)
    {
        if (now >= start && now < end)
        {
            return "正在进行中";
        }

        if (now >= end)
        {
            return "已完成";
        }

        return fallback;
    }

    private static string ResolveCoachDisplayStatus(CoachSchedule schedule, DateTime now)
    {
        if (IsCancelledCoachSchedule(schedule))
        {
            return "已取消";
        }

        return ResolveDisplayStatus(
            schedule.ScheduleStart,
            schedule.ScheduleEnd,
            now,
            fallback: string.IsNullOrWhiteSpace(schedule.Status) ? "正常" : schedule.Status.Trim());
    }

    private static CoachScheduleDto MapToDto(
        CoachSchedule schedule,
        bool isConflict,
        IReadOnlyDictionary<int, PtScheduleDetail> ptDetails,
        IReadOnlyDictionary<int, string> groupCourseNames,
        DateTime now)
    {
        int? memberId = null;
        string? memberName = null;
        string? courseName = null;

        if (schedule.SourceRecordId is int sourceId)
        {
            if (string.Equals(schedule.ScheduleType, "P", StringComparison.OrdinalIgnoreCase)
                && ptDetails.TryGetValue(sourceId, out var detail))
            {
                memberId = detail.MemberId;
                memberName = detail.MemberName;
                courseName = detail.CourseName;
            }
            else if (string.Equals(schedule.ScheduleType, "G", StringComparison.OrdinalIgnoreCase)
                && groupCourseNames.TryGetValue(sourceId, out var name))
            {
                courseName = name;
            }
        }

        return new CoachScheduleDto
        {
            ScheduleId = schedule.ScheduleId,
            CoachId = schedule.CoachId,
            ScheduleStart = schedule.ScheduleStart,
            ScheduleEnd = schedule.ScheduleEnd,
            ScheduleDate = schedule.ScheduleDate,
            ScheduleType = schedule.ScheduleType,
            SourceRecordId = schedule.SourceRecordId,
            Status = ResolveCoachDisplayStatus(schedule, now),
            IsConflict = isConflict,
            MemberId = memberId,
            MemberName = memberName,
            CourseName = courseName
        };
    }
}
