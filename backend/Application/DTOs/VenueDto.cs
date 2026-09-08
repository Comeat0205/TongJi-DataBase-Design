namespace Application.DTOs;

public sealed class VenueDto
{
    public int VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public short MaxCapacity { get; init; }
    public short? CurrentCapacity { get; init; }
    public string? ImageUrl { get; init; }
    public string? VenueStatus { get; init; }
}
