using Domain.Entities;

namespace Domain.Interfaces;

public interface IVenueRepository : IRepository<Venue, int>
{
    // feature/venue-checkin 入场与容量模块
    Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken ct = default);

    // feature/basic-info 基本信息模块
    Task<IReadOnlyList<Venue>> GetManagementListAsync(string? keyword, string? status, CancellationToken cancellationToken = default);
    Task<int> GetNextVenueIdAsync(CancellationToken cancellationToken = default);
}
