namespace Application.DTOs;

public sealed class CheckInOutDto
{
    public int CheckInOutId { get; init; }
    public int VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public int? CardId { get; init; }
    public int? MemberId { get; init; }
    public string? MemberName { get; init; }
    public DateTime CheckInTime { get; init; }
    public DateTime? CheckOutTime { get; init; }
    public string? CheckOutMode { get; init; }
}
