namespace Application.DTOs;

public sealed class CapacityMovementDto
{
    public int MovementId { get; init; }

    public int VenueId { get; init; }

    public string VenueName { get; init; } = string.Empty;

    public int MemberId { get; init; }

    public DateTime EventTime { get; init; }

    /// <summary>进场 / 出场</summary>
    public string EventTypeLabel { get; init; } = string.Empty;

    /// <summary>0=进场，1=出场</summary>
    public string EventType { get; init; } = "0";

    public int RecordedCount { get; init; }

    public decimal OccupancyRate { get; init; }
}
