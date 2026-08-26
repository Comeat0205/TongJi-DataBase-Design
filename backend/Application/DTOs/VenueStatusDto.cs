namespace Application.DTOs;

public sealed class VenueStatusDto
{
    public int VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public int MaxCapacity { get; init; }
    public int CurrentCapacity { get; init; }
    public decimal OccupancyRate { get; init; }
    public string VenueStatus { get; init; } = string.Empty;
}
