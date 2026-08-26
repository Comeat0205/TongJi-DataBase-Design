using Domain.Entities;

namespace Domain.Interfaces;

public interface IVenueRepository : IRepository<Venue, int>
{
    Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken ct = default);
}
