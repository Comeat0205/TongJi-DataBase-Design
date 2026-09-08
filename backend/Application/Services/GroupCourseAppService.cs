using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public class GroupCourseAppService : IGroupCourseAppService
{
    private readonly IGroupcourseRepository _groupcourseRepository;
    private readonly ICourseTypeRepository _courseTypeRepository;
    private readonly ICoachRepository _coachRepository;
    private readonly ITimeSlotRepository _timeSlotRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupCourseScheduleRepository _groupCourseScheduleRepository;

    public GroupCourseAppService(
        IGroupcourseRepository groupcourseRepository,
        ICourseTypeRepository courseTypeRepository,
        ICoachRepository coachRepository,
        ITimeSlotRepository timeSlotRepository,
        IUnitOfWork unitOfWork,
        IGroupCourseScheduleRepository groupCourseScheduleRepository)
    {
        _groupcourseRepository = groupcourseRepository;
        _courseTypeRepository = courseTypeRepository;
        _coachRepository = coachRepository;
        _timeSlotRepository = timeSlotRepository;
        _unitOfWork = unitOfWork;
        _groupCourseScheduleRepository = groupCourseScheduleRepository;
    }

    public async Task<IReadOnlyList<GroupCourseDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var courses = await _groupcourseRepository.GetAllAsync(cancellationToken);

        return courses
            .Select(ToDto)
            .ToList();
    }

    public async Task<(bool Success, GroupCourseDto? Data, string Message)> CreateAsync(
        GroupCourseRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.CourseId <= 0)
        {
            return (false, null, "团课ID必须大于0");
        }

        var courseName = request.CourseName?.Trim();

        if (string.IsNullOrWhiteSpace(courseName))
        {
            return (false, null, "团课名称不能为空");
        }

        if (request.MaxCapacity <= 0)
        {
            return (false, null, "最大容量必须大于0");
        }

        if (request.TypeId <= 0)
        {
            return (false, null, "课程类型不能为空");
        }

        if (request.CoachId <= 0)
        {
            return (false, null, "授课教练不能为空");
        }

        var existing = await _groupcourseRepository.GetByIdAsync(
            request.CourseId,
            cancellationToken);

        if (existing is not null)
        {
            return (false, null, "团课ID已存在");
        }

        var courseType = await _courseTypeRepository.GetByIdAsync(
            request.TypeId,
            cancellationToken);

        if (courseType is null)
        {
            return (false, null, "课程类型不存在");
        }

        var coach = await _coachRepository.GetByIdAsync(
            request.CoachId,
            cancellationToken);

        if (coach is null)
        {
            return (false, null, "教练不存在");
        }

        if (!string.IsNullOrWhiteSpace(coach.Status) &&
            coach.Status != "在职")
        {
            return (false, null, "授课教练当前不是在职状态");
        }

        if (request.Weekday is >= 1 and <= 7
            && !string.IsNullOrWhiteSpace(request.StartTime)
            && !string.IsNullOrWhiteSpace(request.EndTime)
            && request.ScheduleFrom is not null
            && request.ScheduleTo is not null)
        {
            var conflict = await CheckScheduleConflictAsync(
                new GroupCourseScheduleConflictRequestDto
                {
                    CourseId = request.CourseId,
                    CoachId = request.CoachId,
                    RangeStart = request.ScheduleFrom.Value.Date,
                    RangeEnd = request.ScheduleTo.Value.Date,
                    Weekday = request.Weekday,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                },
                cancellationToken);

            if (!conflict.Success)
            {
                return (false, null, conflict.Message);
            }
        }

        var scheduleResult = await EnsureWeeklyScheduleAsync(
            request.CourseId,
            request,
            requireSchedule: true,
            cancellationToken);

        if (!scheduleResult.Success)
        {
            return (false, null, scheduleResult.Message);
        }

        var entity = new Groupcourse
        {
            CourseId = request.CourseId,
            CourseName = courseName,
            MaxCapacity = request.MaxCapacity,
            CurrentCapacity = 0,
            CourseSummary = request.CourseSummary?.Trim(),
            TypeId = request.TypeId,
            CoachId = request.CoachId,
            TimeSlotId = scheduleResult.TimeSlotId!,
        };

        await _groupcourseRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var courses = await _groupcourseRepository.GetAllAsync(cancellationToken);
        var created = courses.First(x => x.CourseId == entity.CourseId);

        return (true, ToDto(created), "创建成功");
    }

    public async Task<(bool Success, GroupCourseDto? Data, string Message)> UpdateAsync(
        int courseId,
        GroupCourseRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var courseName = request.CourseName?.Trim();

        if (string.IsNullOrWhiteSpace(courseName))
        {
            return (false, null, "团课名称不能为空");
        }

        if (request.MaxCapacity <= 0)
        {
            return (false, null, "最大容量必须大于0");
        }

        if (request.TypeId <= 0)
        {
            return (false, null, "课程类型不能为空");
        }

        if (request.CoachId <= 0)
        {
            return (false, null, "授课教练不能为空");
        }

        var entity = await _groupcourseRepository.GetByIdAsync(
            courseId,
            cancellationToken);

        if (entity is null)
        {
            return (false, null, "团课不存在");
        }

        var courseType = await _courseTypeRepository.GetByIdAsync(
            request.TypeId,
            cancellationToken);

        if (courseType is null)
        {
            return (false, null, "课程类型不存在");
        }

        var coach = await _coachRepository.GetByIdAsync(
            request.CoachId,
            cancellationToken);

        if (coach is null)
        {
            return (false, null, "教练不存在");
        }

        if (!string.IsNullOrWhiteSpace(coach.Status) &&
            coach.Status != "在职")
        {
            return (false, null, "授课教练当前不是在职状态");
        }

        if (request.MaxCapacity < (entity.CurrentCapacity ?? 0))
        {
            return (
                false,
                null,
                $"最大容量不能小于当前报名人数 {entity.CurrentCapacity ?? 0}");
        }

        var hasWeeklyInput = request.Weekday is >= 1 and <= 7
            && !string.IsNullOrWhiteSpace(request.StartTime)
            && !string.IsNullOrWhiteSpace(request.EndTime)
            && request.ScheduleFrom is not null
            && request.ScheduleTo is not null;

        string timeSlotId;
        if (hasWeeklyInput)
        {
            request.CourseId = courseId;
            var conflict = await CheckScheduleConflictAsync(
                new GroupCourseScheduleConflictRequestDto
                {
                    CourseId = courseId,
                    CoachId = request.CoachId,
                    RangeStart = request.ScheduleFrom!.Value.Date,
                    RangeEnd = request.ScheduleTo!.Value.Date,
                    Weekday = request.Weekday,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                },
                cancellationToken);

            if (!conflict.Success)
            {
                return (false, null, conflict.Message);
            }

            var scheduleResult = await EnsureWeeklyScheduleAsync(
                courseId,
                request,
                requireSchedule: true,
                cancellationToken);

            if (!scheduleResult.Success)
            {
                return (false, null, scheduleResult.Message);
            }

            timeSlotId = scheduleResult.TimeSlotId!;
        }
        else if (!string.IsNullOrWhiteSpace(request.TimeSlotId))
        {
            timeSlotId = request.TimeSlotId.Trim();
            var template = await _timeSlotRepository.GetTemplateAsync(timeSlotId, cancellationToken);
            if (template is null)
            {
                return (false, null, "时间模板不存在，请重新设置上课时间");
            }
        }
        else if (!string.IsNullOrWhiteSpace(entity.TimeSlotId))
        {
            timeSlotId = entity.TimeSlotId;
        }
        else
        {
            return (false, null, "请设置上课时间（星期几、时段与排期区间）");
        }

        entity.CourseName = courseName;
        entity.MaxCapacity = request.MaxCapacity;
        entity.CourseSummary = request.CourseSummary?.Trim();
        entity.TypeId = request.TypeId;
        entity.CoachId = request.CoachId;
        entity.TimeSlotId = timeSlotId;

        _groupcourseRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var courses = await _groupcourseRepository.GetAllAsync(cancellationToken);
        var updated = courses.First(x => x.CourseId == courseId);

        return (true, ToDto(updated), "修改成功");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(
        int courseId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _groupcourseRepository.GetByIdAsync(courseId, cancellationToken);
        if (entity is null)
        {
            return (false, "团课不存在");
        }

        _groupcourseRepository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (true, "删除成功");
    }

    public async Task<(bool Success, string Message)> CheckScheduleConflictAsync(
        GroupCourseScheduleConflictRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.CoachId <= 0)
        {
            return (false, "教练ID必须大于0");
        }

        if (request.RangeStart == default || request.RangeEnd == default)
        {
            return (false, "请选择排课日期区间");
        }

        if (request.RangeEnd.Date < request.RangeStart.Date)
        {
            return (false, "结束日期不能早于开始日期");
        }

        var coach = await _coachRepository.GetByIdAsync(
            request.CoachId,
            cancellationToken);

        if (coach is null)
        {
            return (false, "教练不存在");
        }

        if (!string.IsNullOrWhiteSpace(coach.Status) &&
            coach.Status != "在职")
        {
            return (false, "授课教练当前不是在职状态");
        }

        var hasDraftPattern = request.Weekday is >= 1 and <= 7
            && !string.IsNullOrWhiteSpace(request.StartTime)
            && !string.IsNullOrWhiteSpace(request.EndTime);

        if (hasDraftPattern)
        {
            if (!TryParseHm(request.StartTime!, out var startTod)
                || !TryParseHm(request.EndTime!, out var endTod))
            {
                return (false, "开始/结束时间格式应为 HH:mm");
            }

            if (endTod <= startTod)
            {
                return (false, "结束时间必须晚于开始时间");
            }

            return await _groupCourseScheduleRepository.CheckWeeklyConflictWithPatternAsync(
                request.CourseId,
                request.CoachId,
                request.Weekday!.Value,
                startTod,
                endTod,
                request.RangeStart,
                request.RangeEnd,
                cancellationToken);
        }

        if (request.CourseId <= 0)
        {
            return (false, "团课ID必须大于0，或请提供星期几与上课时段做草稿检测");
        }

        var course = await _groupcourseRepository.GetByIdAsync(
            request.CourseId,
            cancellationToken);

        if (course is null)
        {
            return (false, "团课不存在");
        }

        return await _groupCourseScheduleRepository.CheckWeeklyConflictInRangeAsync(
            request.CourseId,
            request.CoachId,
            request.RangeStart,
            request.RangeEnd,
            cancellationToken);
    }

    /// <summary>
    /// 按「星期几 + 时段 + 日期区间」写入 TIME_SLOT_TEMPLATE / TIME_SLOT_INSTANCE。
    /// 每门团课使用独立模板 ID：GC_{courseId}（不超过 20 字符）。
    /// </summary>
    private async Task<(bool Success, string? TimeSlotId, string Message)> EnsureWeeklyScheduleAsync(
        int courseId,
        GroupCourseRequestDto request,
        bool requireSchedule,
        CancellationToken cancellationToken)
    {
        var hasWeeklyInput = request.Weekday is >= 1 and <= 7
            && !string.IsNullOrWhiteSpace(request.StartTime)
            && !string.IsNullOrWhiteSpace(request.EndTime)
            && request.ScheduleFrom is not null
            && request.ScheduleTo is not null;

        if (!hasWeeklyInput)
        {
            if (!requireSchedule && !string.IsNullOrWhiteSpace(request.TimeSlotId))
            {
                return (true, request.TimeSlotId.Trim(), "ok");
            }

            return (false, null, "请设置上课时间：星期几、开始/结束时间，以及排期起止日期");
        }

        if (!TryParseHm(request.StartTime!, out var startTod)
            || !TryParseHm(request.EndTime!, out var endTod))
        {
            return (false, null, "开始/结束时间格式应为 HH:mm");
        }

        if (endTod <= startTod)
        {
            return (false, null, "结束时间必须晚于开始时间");
        }

        var from = request.ScheduleFrom!.Value.Date;
        var to = request.ScheduleTo!.Value.Date;
        if (to < from)
        {
            return (false, null, "排期结束日期不能早于开始日期");
        }

        var targetDow = ToDayOfWeek(request.Weekday!.Value);
        var timeSlotId = $"GC_{courseId}";
        if (timeSlotId.Length > 20)
        {
            timeSlotId = $"GC{courseId}";
        }

        var template = await _timeSlotRepository.GetTemplateAsync(timeSlotId, cancellationToken);
        if (template is null)
        {
            await _timeSlotRepository.AddTemplateAsync(
                new TimeSlotTemplate { TimeSlotId = timeSlotId },
                cancellationToken);
        }

        await _timeSlotRepository.RemoveInstancesAsync(timeSlotId, cancellationToken);

        var instances = new List<TimeSlotInstance>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            if (day.DayOfWeek != targetDow)
            {
                continue;
            }

            instances.Add(new TimeSlotInstance
            {
                TimeSlotId = timeSlotId,
                CourseDate = day,
                StartTime = day.Add(startTod),
                EndTime = day.Add(endTod),
            });
        }

        if (instances.Count == 0)
        {
            return (false, null, "所选日期区间内没有对应的星期几，请扩大排期区间");
        }

        await _timeSlotRepository.AddInstancesAsync(instances, cancellationToken);
        return (true, timeSlotId, $"已生成 {instances.Count} 个上课日");
    }

    private static bool TryParseHm(string value, out TimeSpan time)
    {
        time = default;
        var parts = value.Trim().Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var h) || !int.TryParse(parts[1], out var m))
        {
            return false;
        }

        if (h is < 0 or > 23 || m is < 0 or > 59)
        {
            return false;
        }

        time = new TimeSpan(h, m, 0);
        return true;
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

    private static GroupCourseDto ToDto(Groupcourse entity)
    {
        return new GroupCourseDto
        {
            CourseId = entity.CourseId,
            CourseName = entity.CourseName,
            MaxCapacity = entity.MaxCapacity,
            CurrentCapacity = entity.CurrentCapacity ?? 0,
            CourseSummary = entity.CourseSummary,

            TypeId = entity.TypeId,
            CourseTypeName = entity.Type.TypeName,

            CoachId = entity.CoachId,
            CoachName = entity.Coach.CoachName,

            TimeSlotId = entity.TimeSlotId,

            TimeSlots = entity.TimeSlot.TimeSlotInstances
                .OrderBy(t => t.CourseDate)
                .ThenBy(t => t.StartTime)
                .Select(t => new GroupCourseTimeSlotDto
                {
                    CourseDate = t.CourseDate,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime
                })
                .ToList()
        };
    }
}
