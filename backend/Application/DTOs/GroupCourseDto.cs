namespace Application.DTOs;

public sealed class GroupCourseDto
{
    public int CourseId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public short MaxCapacity { get; set; }

    public short CurrentCapacity { get; set; }

    public string? CourseSummary { get; set; }

    public int TypeId { get; set; }

    public int CoachId { get; set; }

    public string TimeSlotId { get; set; } = string.Empty;
}
