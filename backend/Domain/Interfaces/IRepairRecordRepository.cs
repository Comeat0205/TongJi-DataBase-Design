using Domain.Entities;

namespace Domain.Interfaces;

public interface IRepairRecordRepository : IRepository<Repairrecord, int>
{
    Task<IReadOnlyList<Repairrecord>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? status,
        CancellationToken cancellationToken = default);
}
