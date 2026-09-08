namespace Application.DTOs;

public sealed class CheckInResultDto
{
    public int CheckInOutId { get; init; }
    public string MemberName { get; init; } = string.Empty;
    public string VenueName { get; init; } = string.Empty;
    public DateTime CheckInTime { get; init; }
    public string CardType { get; init; } = string.Empty;
    public string CardStatus { get; init; } = string.Empty;
    public int? RemainingCount { get; init; }
    public DateTime? ExpireDate { get; init; }
    /// <summary>
    /// 入场后场馆容量预警级别：normal / warning(≥90%) / full
    /// </summary>
    public string CapacityWarningLevel { get; init; } = "normal";
}
