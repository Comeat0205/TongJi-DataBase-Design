namespace Application.DTOs;

public sealed class UpdateEquipmentRequestDto
{
    public string EquipName { get; init; } = string.Empty;
    public int? VenueId { get; init; }
    public string? ImageUrl { get; init; }
    public string Status { get; init; } = string.Empty;
}
