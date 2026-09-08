namespace Application.DTOs;

public sealed class MemberScheduleDto
{
    public int ScheduleId { get; init; }
    public int MemberId { get; init; }
    public DateTime ScheduleStart { get; init; }
    public DateTime ScheduleDate { get; init; }
    public DateTime ScheduleEnd { get; init; }
    public string ScheduleType { get; init; } = string.Empty;
    public int? SourceRecordId { get; init; }
    public string? Status { get; init; }
    public bool IsUpcoming { get; init; }

    /// <summary>课程名称（私教来自 PERSONAL_COURSE；团操来自 GROUPCOURSE）。</summary>
    public string? CourseName { get; init; }

    /// <summary>授课教练编号。</summary>
    public int? CoachId { get; init; }

    /// <summary>授课教练姓名。</summary>
    public string? CoachName { get; init; }
}
