namespace Application.DTOs;

public sealed class UpdateVenueRequestDto
{
    public string VenueName { get; init; } = string.Empty;
    public short MaxCapacity { get; init; }
    public string VenueStatus { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
}
