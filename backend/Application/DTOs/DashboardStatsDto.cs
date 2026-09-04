namespace Application.DTOs;

/// <summary>
/// 员工首页运营统计摘要
/// </summary>
public sealed class DashboardStatsDto
{
    /// <summary>今日入场人次</summary>
    public int TodayCheckIns { get; init; }

    /// <summary>当前在场总人数</summary>
    public int ActiveMembers { get; init; }

    /// <summary>场馆列表（实时容量）</summary>
    public IReadOnlyList<VenueStatusDto> Venues { get; init; } = Array.Empty<VenueStatusDto>();
}
