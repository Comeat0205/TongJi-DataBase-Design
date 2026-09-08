namespace Application.DTOs;

public sealed class CoachScheduleDto
{
    public int ScheduleId { get; init; }
    public int CoachId { get; init; }
    public DateTime ScheduleStart { get; init; }
    public DateTime ScheduleEnd { get; init; }
    public DateTime ScheduleDate { get; init; }
    public string? ScheduleType { get; init; }
    public int? SourceRecordId { get; init; }
    public string? Status { get; init; }
    public bool IsConflict { get; init; }

    /// <summary>预约会员编号（私教来自 PTBOOKING）。</summary>
    public int? MemberId { get; init; }

    /// <summary>预约会员姓名。</summary>
    public string? MemberName { get; init; }

    /// <summary>课程名称（私教课程名）。</summary>
    public string? CourseName { get; init; }
}
