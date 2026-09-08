namespace Application.DTOs;

public sealed class GroupCourseScheduleConflictRequestDto
{
    public int CourseId { get; set; }
    public int CoachId { get; set; }

    /// <summary>意向排课区间起（含）。</summary>
    public DateTime RangeStart { get; set; }

    /// <summary>意向排课区间止（含）。</summary>
    public DateTime RangeEnd { get; set; }
}
