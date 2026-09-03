using Domain.Entities;

namespace Domain.Interfaces;

public interface IVenueRepository : IRepository<Venue, int>
{
    Task<IReadOnlyList<Venue>> GetManagementListAsync(string? keyword, string? status, CancellationToken cancellationToken = default);
    Task<int> GetNextVenueIdAsync(CancellationToken cancellationToken = default);
}
