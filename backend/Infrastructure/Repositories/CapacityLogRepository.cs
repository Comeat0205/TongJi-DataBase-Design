using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class CapacityLogRepository : Repository<Capacitylog, int>, ICapacityLogRepository
{
    public CapacityLogRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Capacitylog>> GetPagedAsync(int venueId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(x => x.Venue)
            .AsQueryable();

        if (venueId > 0)
        {
            query = query.Where(x => x.VenueId == venueId);
        }

        return await query
            .OrderByDescending(x => x.LogTimestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextIdAsync(CancellationToken cancellationToken = default)
    {
        var maxId = await DbSet
            .AsNoTracking()
            .MaxAsync(x => (int?)x.CapacityLogId, cancellationToken);
        return (maxId ?? 0) + 1;
    }
}
