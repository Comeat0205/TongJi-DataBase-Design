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
}
