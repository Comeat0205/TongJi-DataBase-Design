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
    Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken ct = default);
    Task<string> TriggerAutoCheckoutAsync(CancellationToken ct = default);
    Task<CheckInOutDto?> GetMyActiveCheckInAsync(int cardId, CancellationToken ct = default);
    Task<MemberCardDto?> GetMemberCardAsync(int cardId, CancellationToken ct = default);
}
