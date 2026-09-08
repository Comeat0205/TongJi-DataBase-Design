namespace Application.DTOs;

public sealed class CapacityLogPointDto
{
    public DateTime Timestamp { get; init; }

    /// <summary>HH:mm，便于前端画轴。</summary>
    public string TimeLabel { get; init; } = string.Empty;

    public int RecordedCount { get; init; }

    public decimal OccupancyRate { get; init; }

    public int? RecordedCapacity { get; init; }
}

/// <summary>主训练馆某日容量采样序列（约每 10 分钟一个点）。</summary>
public sealed class CapacityDailySeriesDto
{
    public int VenueId { get; init; }

    public string VenueName { get; init; } = string.Empty;

    /// <summary>yyyy-MM-dd</summary>
    public string Date { get; init; } = string.Empty;

    public int MaxCapacity { get; init; }

    public IReadOnlyList<CapacityLogPointDto> Points { get; init; } = Array.Empty<CapacityLogPointDto>();
}
