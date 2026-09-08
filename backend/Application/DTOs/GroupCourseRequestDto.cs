namespace Application.DTOs;

public sealed class GroupCourseRequestDto
{
    public int CourseId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public short MaxCapacity { get; set; }

    public string? CourseSummary { get; set; }

    public int TypeId { get; set; }

    public int CoachId { get; set; }

    /// <summary>兼容旧数据；新流程可不传，由周课字段自动生成 GC_{courseId}。</summary>
    public string? TimeSlotId { get; set; }

    /// <summary>1=周一 … 7=周日。</summary>
    public int? Weekday { get; set; }

    /// <summary>HH:mm</summary>
    public string? StartTime { get; set; }

    /// <summary>HH:mm</summary>
    public string? EndTime { get; set; }

    /// <summary>生成 TIME_SLOT_INSTANCE 的日期区间起（含）。</summary>
    public DateTime? ScheduleFrom { get; set; }

    /// <summary>生成 TIME_SLOT_INSTANCE 的日期区间止（含）。</summary>
    public DateTime? ScheduleTo { get; set; }
}
