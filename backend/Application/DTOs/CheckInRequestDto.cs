namespace Application.DTOs;

public sealed class CheckInRequestDto
{
    public int CardId { get; init; }
    public int VenueId { get; init; } = 1;
}
