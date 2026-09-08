namespace Application.DTOs;

public sealed class UpdateEquipmentRequestDto
{
    public string EquipName { get; init; } = string.Empty;
    public int? VenueId { get; init; }
    public string? ImageUrl { get; init; }
    public string Status { get; init; } = string.Empty;

    /// <summary>状态改为维修时必填，用于自动生成器材报修记录。</summary>
    public string? FaultDescription { get; init; }
}
