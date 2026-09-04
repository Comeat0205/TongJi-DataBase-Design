namespace Application.DTOs;

public sealed class CapacityLogDto
{
    public int CapacityLogId { get; init; }
    public int VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public DateTime? LogTimestamp { get; init; }
    public int? RecordedCapacity { get; init; }
    public int RecordedCount { get; init; }
    public decimal? OccupancyRate { get; init; }
}
