namespace Application.DTOs;

public sealed class EquipmentDto
{
    public int EquipId { get; init; }
    public string EquipName { get; init; } = string.Empty;
    public int? VenueId { get; init; }
    public string? ImageUrl { get; init; }
    public string? Status { get; init; }
    public DateTime? PurchaseDate { get; init; }
}
