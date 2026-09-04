namespace Application.DTOs;

public sealed class VenueStatusDto
{
    public int VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public int MaxCapacity { get; init; }
    public int CurrentCapacity { get; init; }
    public decimal OccupancyRate { get; init; }
    public string VenueStatus { get; init; } = string.Empty;
    /// <summary>
    /// 容量预警级别：normal(正常) / warning(≥90% 黄灯预警) / full(已满)
    /// </summary>
    public string CapacityWarningLevel { get; init; } = "normal";
}
