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

    public ScheduleAppService(
        IMemberScheduleRepository memberScheduleRepository,
        ICoachScheduleRepository coachScheduleRepository,
        IPtBookingRepository ptBookingRepository,
        IGroupCourseBookingRepository groupCourseBookingRepository)
    {
        _memberScheduleRepository = memberScheduleRepository;
        _coachScheduleRepository = coachScheduleRepository;
        _ptBookingRepository = ptBookingRepository;
        _groupCourseBookingRepository = groupCourseBookingRepository;
    }

    public async Task<IReadOnlyList<MemberScheduleDto>> GetMemberSchedulesAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var schedules = await _memberScheduleRepository.GetByMemberIdAsync(memberId, cancellationToken);
        // 与私教预约一致：按本地墙钟比较时段状态。
        var now = DateTime.Now;

        // 已取消、已过结束时间的不展示；只保留未开始或进行中。
        var activeSchedules = schedules
            .Where(s => !IsCancelledMemberSchedule(s) && s.ScheduleEnd > now)
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
        var allSchedules = await _coachScheduleRepository.GetByCoachIdAsync(coachId, cancellationToken);

        // 已取消、已过结束时间的不展示；只保留未开始或进行中的日程。
        var activeSchedules = allSchedules
            .Where(s => !IsCancelledCoachSchedule(s) && s.ScheduleEnd > now)
            .ToList();

        var conflictIds = FindConflictIds(activeSchedules);

        var ptIds = activeSchedules
            .Where(s => string.Equals(s.ScheduleType, "P", StringComparison.OrdinalIgnoreCase)
                && s.SourceRecordId is > 0)
            .Select(s => s.SourceRecordId!.Value)
            .Distinct()
            .ToList();

        var ptDetails = await _ptBookingRepository.GetScheduleDetailsByIdsAsync(ptIds, cancellationToken);

        return activeSchedules
            .Select(s => MapToDto(s, conflictIds.Contains(s.ScheduleId), ptDetails, now))
            .ToList();
    }

    // 按时段计算展示状态：时段前「待上课」，时段内「上课中」；结束后不返回。
    private static MemberScheduleDto MapToDto(
        MemberSchedule schedule,
        DateTime now,
        IReadOnlyDictionary<int, PtScheduleDetail> ptDetails,
        IReadOnlyDictionary<int, (string CourseName, int CoachId, string CoachName)> groupDetails)
    {
        var displayStatus = now < schedule.ScheduleStart
            ? "待上课"
            : "上课中";

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
            .Where(b => bookingIds.Contains(b.BookingId) && b.Course is not null)
            .ToDictionary(
                b => b.BookingId,
                b => (
                    b.Course.CourseName,
                    b.Course.CoachId,
                    b.Course.Coach?.CoachName ?? $"教练 #{b.Course.CoachId}"
                ));
    }

    // 功能点 #13：教练自身日程时间重叠（start < 另一条 end 且 end > 另一条 start）。
    private static HashSet<int> FindConflictIds(IReadOnlyList<CoachSchedule> schedules)
    {
        var conflicts = new HashSet<int>();
        for (var i = 0; i < schedules.Count; i++)
        {
            for (var j = i + 1; j < schedules.Count; j++)
            {
                var a = schedules[i];
                var b = schedules[j];
                if (a.ScheduleStart < b.ScheduleEnd && a.ScheduleEnd > b.ScheduleStart)
                {
                    conflicts.Add(a.ScheduleId);
                    conflicts.Add(b.ScheduleId);
                }
            }
        }

        return conflicts;
    }

    private static bool IsCancelledCoachSchedule(CoachSchedule schedule)
    {
        var status = schedule.Status?.Trim();
        return string.Equals(status, "已取消", StringComparison.Ordinal)
            || string.Equals(status, "2", StringComparison.Ordinal);
    }

    private static string ResolveCoachDisplayStatus(CoachSchedule schedule, DateTime now)
    {
        if (IsCancelledCoachSchedule(schedule))
        {
            return "已取消";
        }

        if (now >= schedule.ScheduleStart && now < schedule.ScheduleEnd)
        {
            return "正在进行中";
        }

        if (now >= schedule.ScheduleEnd)
        {
            return "已完成";
        }

        return string.IsNullOrWhiteSpace(schedule.Status) ? "正常" : schedule.Status.Trim();
    }

    private static CoachScheduleDto MapToDto(
        CoachSchedule schedule,
        bool isConflict,
        IReadOnlyDictionary<int, PtScheduleDetail> ptDetails,
        DateTime now)
    {
        int? memberId = null;
        string? memberName = null;
        string? courseName = null;

        if (string.Equals(schedule.ScheduleType, "P", StringComparison.OrdinalIgnoreCase)
            && schedule.SourceRecordId is int sourceId
            && ptDetails.TryGetValue(sourceId, out var detail))
        {
            memberId = detail.MemberId;
            memberName = detail.MemberName;
            courseName = detail.CourseName;
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
