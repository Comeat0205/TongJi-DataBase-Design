using Domain.Entities;

namespace Domain.Interfaces;

public interface IEquipmentRepository : IRepository<Equipment, int>
{
    Task<IReadOnlyList<Equipment>> GetManagementListAsync(string? keyword, string? status, int? venueId, CancellationToken cancellationToken = default);
    Task<int> GetNextEquipmentIdAsync(CancellationToken cancellationToken = default);
}
