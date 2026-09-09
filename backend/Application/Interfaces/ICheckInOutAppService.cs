using Application.DTOs;

namespace Application.Interfaces;

public interface ICheckInOutAppService
{
    Task<CheckInResultDto> CheckInAsync(CheckInRequestDto req, CancellationToken ct = default);
    Task<CheckInOutDto?> CheckOutAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<VenueStatusDto>> GetVenueStatusAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CheckInOutDto>> GetActiveCheckInsAsync(int venueId, CancellationToken ct = default);
    Task<IReadOnlyList<CheckInOutDto>> GetPagedAsync(int venueId, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<CapacityLogDto>> GetCapacityLogsPagedAsync(int venueId, int pageNumber, int pageSize, CancellationToken ct = default);

    /// <summary>主训练馆指定日期的容量采样序列（供波形图）。</summary>
    Task<CapacityDailySeriesDto> GetMainVenueDailySeriesAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>主训练馆指定日期的签到/签退流水。</summary>
    Task<IReadOnlyList<CapacityMovementDto>> GetMainVenueMovementsAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>按 10 分钟对齐写入主训练馆实时人数/占用率快照。</summary>
    Task<CapacityLogDto?> RecordMainVenueSnapshotAsync(CancellationToken ct = default);

    Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken ct = default);
    Task<string> TriggerAutoCheckoutAsync(CancellationToken ct = default);
    Task<CheckInOutDto?> GetMyActiveCheckInAsync(int cardId, CancellationToken ct = default);
    Task<CheckInOutDto?> GetMyActiveCheckInByMemberAsync(int memberId, CancellationToken ct = default);
    Task<MemberCardDto?> GetMemberCardAsync(int cardId, CancellationToken ct = default);
}
