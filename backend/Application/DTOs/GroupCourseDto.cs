namespace Application.DTOs;

public sealed class GroupCourseDto
{
    public int CourseId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public short MaxCapacity { get; set; }

    public short CurrentCapacity { get; set; }

    public string? CourseSummary { get; set; }

    public int TypeId { get; set; }

    public string CourseTypeName { get; set; } = string.Empty;

    public int CoachId { get; set; }

    public string CoachName { get; set; } = string.Empty;

    public string TimeSlotId { get; set; } = string.Empty;

    public IReadOnlyList<GroupCourseTimeSlotDto> TimeSlots { get; set; }
        = Array.Empty<GroupCourseTimeSlotDto>();
}

public sealed class GroupCourseTimeSlotDto
{
    public DateTime CourseDate { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }
}