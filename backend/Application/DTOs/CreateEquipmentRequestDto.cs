namespace Application.DTOs;

public sealed class CreateEquipmentRequestDto
{
    public string EquipName { get; init; } = string.Empty;
    public int? VenueId { get; init; }
    public string? ImageUrl { get; init; }
}
