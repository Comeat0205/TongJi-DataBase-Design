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
    private readonly IUnitOfWork _unitOfWork;
private readonly IGroupCourseScheduleRepository _groupCourseScheduleRepository;

    public GroupCourseAppService(
        IGroupcourseRepository groupcourseRepository,
        ICourseTypeRepository courseTypeRepository,
        ICoachRepository coachRepository,
IUnitOfWork unitOfWork,
IGroupCourseScheduleRepository groupCourseScheduleRepository)
    {
        _groupcourseRepository = groupcourseRepository;
        _courseTypeRepository = courseTypeRepository;
        _coachRepository = coachRepository;
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

        var timeSlotId = request.TimeSlotId?.Trim();

        if (string.IsNullOrWhiteSpace(timeSlotId))
        {
            return (false, null, "时间模板不能为空");
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
            return (false, null, "授课教练不存在");
        }

        if (!string.IsNullOrWhiteSpace(coach.Status) &&
            coach.Status != "在职")
        {
            return (false, null, "授课教练当前不是在职状态");
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
            TimeSlotId = timeSlotId
        };

        await _groupcourseRepository.AddAsync(
            entity,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var courses = await _groupcourseRepository.GetAllAsync(
            cancellationToken);

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

        var timeSlotId = request.TimeSlotId?.Trim();

        if (string.IsNullOrWhiteSpace(timeSlotId))
        {
            return (false, null, "时间模板不能为空");
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
            return (false, null, "授课教练不存在");
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

        entity.CourseName = courseName;
        entity.MaxCapacity = request.MaxCapacity;
        entity.CourseSummary = request.CourseSummary?.Trim();
        entity.TypeId = request.TypeId;
        entity.CoachId = request.CoachId;
        entity.TimeSlotId = timeSlotId;

        _groupcourseRepository.Update(entity);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var courses = await _groupcourseRepository.GetAllAsync(
            cancellationToken);

        var updated = courses.First(x => x.CourseId == courseId);

        return (true, ToDto(updated), "修改成功");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(
        int courseId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _groupcourseRepository.GetByIdAsync(
            courseId,
            cancellationToken);

        if (entity is null)
        {
            return (false, "团课不存在");
        }

        if ((entity.CurrentCapacity ?? 0) > 0)
        {
            return (false, "该团课已有报名记录，不能删除");
        }

        _groupcourseRepository.Remove(entity);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (true, "删除成功");
    }

    public async Task<(bool Success, string Message)> CheckScheduleConflictAsync(
    GroupCourseScheduleConflictRequestDto request,
    CancellationToken cancellationToken = default)
{
    if (request.CourseId <= 0)
    {
        return (false, "团课ID必须大于0");
    }

    if (request.CoachId <= 0)
    {
        return (false, "教练ID必须大于0");
    }

    if (request.CourseDate == default)
    {
        return (false, "课程日期不能为空");
    }

    if (request.StartTime >= request.EndTime)
    {
        return (false, "开始时间必须早于结束时间");
    }

    var course = await _groupcourseRepository.GetByIdAsync(
        request.CourseId,
        cancellationToken);

    if (course is null)
    {
        return (false, "团课不存在");
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

    return await _groupCourseScheduleRepository.CheckConflictAsync(
        request.CourseId,
        request.CoachId,
        request.CourseDate,
        request.StartTime,
        request.EndTime,
        cancellationToken);
}

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