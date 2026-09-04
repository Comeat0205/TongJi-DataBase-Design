namespace Application.DTOs;

public sealed class PtBookingDto
{
    public int PtBookingId { get; init; }
    public int PackageId { get; init; }
    public int MemberId { get; init; }
    public int CoachId { get; init; }
    public string CoachName { get; init; } = string.Empty;
    public string CourseName { get; init; } = string.Empty;
    public DateTime BookingTime { get; init; }
    public DateTime SessionTime { get; init; }
    public string CoachConfirmed { get; init; } = string.Empty;
    public string MemberConfirmed { get; init; } = string.Empty;
    public string ConsumeStatus { get; init; } = string.Empty;
    public DateTime? ConsumedTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsConsumed { get; init; }
    public bool CanConsume { get; init; }
    public bool CanUndoConsumption { get; init; }
}
