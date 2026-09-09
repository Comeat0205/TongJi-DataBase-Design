using Domain.Entities;

namespace Domain.Interfaces;

public interface ICapacityMovementRepository : IRepository<CapacityMovement, int>
{
    Task EnsureTableAsync(CancellationToken cancellationToken = default);

    Task<int> GetNextIdAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CapacityMovement>> GetByVenueAndDateAsync(
        int venueId,
        DateTime dayStart,
        DateTime dayEndExclusive,
        CancellationToken cancellationToken = default);
}
