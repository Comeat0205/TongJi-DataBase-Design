using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;

namespace Application.Services;

public class GroupCourseAppService : IGroupCourseAppService
{
    private readonly IGroupcourseRepository _groupcourseRepository;

    public GroupCourseAppService(
        IGroupcourseRepository groupcourseRepository)
    {
        _groupcourseRepository = groupcourseRepository;
    }

    public async Task<IReadOnlyList<GroupCourseDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var courses = await _groupcourseRepository.GetAllAsync(cancellationToken);

        return courses
            .Select(c => new GroupCourseDto
            {
                CourseId = c.CourseId,
                CourseName = c.CourseName,

                MaxCapacity = c.MaxCapacity,
                CurrentCapacity = c.CurrentCapacity ?? 0,

                CourseSummary = c.CourseSummary,

                TypeId = c.TypeId,
                CourseTypeName = c.Type.TypeName,

                CoachId = c.CoachId,
                CoachName = c.Coach.CoachName,

                TimeSlotId = c.TimeSlotId,

                TimeSlots = c.TimeSlot.TimeSlotInstances
                    .OrderBy(t => t.CourseDate)
                    .ThenBy(t => t.StartTime)
                    .Select(t => new GroupCourseTimeSlotDto
                    {
                        CourseDate = t.CourseDate,
                        StartTime = t.StartTime,
                        EndTime = t.EndTime
                    })
                    .ToList()
            })
            .ToList();
    }
}