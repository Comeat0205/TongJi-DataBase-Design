namespace Application.DTOs;

public sealed class GroupCourseRequestDto
{
    public int CourseId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public short MaxCapacity { get; set; }

    public string? CourseSummary { get; set; }

    public int TypeId { get; set; }

    public int CoachId { get; set; }

    public string TimeSlotId { get; set; } = string.Empty;
}
