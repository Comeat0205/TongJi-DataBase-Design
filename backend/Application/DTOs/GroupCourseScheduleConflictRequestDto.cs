namespace Application.DTOs;

public sealed class GroupCourseScheduleConflictRequestDto
{
    public int CourseId { get; set; }
    public int CoachId { get; set; }
    public DateTime CourseDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
