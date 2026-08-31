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
}
