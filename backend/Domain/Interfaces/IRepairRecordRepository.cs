using Domain.Entities;

namespace Domain.Interfaces;

public interface IRepairRecordRepository : IRepository<Repairrecord, int>
{
    Task<Repairrecord?> GetDetailsByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Repairrecord>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? status,
        CancellationToken cancellationToken = default);
    Task<bool> EquipmentExistsAsync(int equipId, CancellationToken cancellationToken = default);
    Task<bool> EmployeeExistsAsync(int empId, CancellationToken cancellationToken = default);
    Task<int> GetNextIdAsync(CancellationToken cancellationToken = default);
}
