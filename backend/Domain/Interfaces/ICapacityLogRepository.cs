using Domain.Entities;

namespace Domain.Interfaces;

public interface ICapacityLogRepository : IRepository<Capacitylog, int>
{
    Task<IReadOnlyList<Capacitylog>> GetPagedAsync(int venueId, int pageNumber, int pageSize, CancellationToken ct = default);

    Task<IReadOnlyList<Capacitylog>> GetByVenueAndDateAsync(
        int venueId,
        DateTime dayStart,
        DateTime dayEndExclusive,
        CancellationToken ct = default);

    Task<bool> ExistsAtAsync(int venueId, DateTime timestamp, CancellationToken ct = default);

    Task<int> GetNextIdAsync(CancellationToken ct = default);
}
