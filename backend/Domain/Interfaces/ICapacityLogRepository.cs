using Domain.Entities;

namespace Domain.Interfaces;

public interface ICapacityLogRepository : IRepository<Capacitylog, int>
{
    Task<IReadOnlyList<Capacitylog>> GetPagedAsync(int venueId, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<int> GetNextIdAsync(CancellationToken ct = default);
}
