namespace Application.DTOs;

public sealed class CreateVenueRequestDto
{
    public string VenueName { get; init; } = string.Empty;
    public short MaxCapacity { get; init; }
    public string? ImageUrl { get; init; }
}
