using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class VenueRepository : Repository<Venue, int>, IVenueRepository
{
    public VenueRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Venue>> GetManagementListAsync(string? keyword, string? status, CancellationToken cancellationToken = default)
    {
        var query = Context.Venues.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            query = query.Where(x =>
                x.VenueName.Contains(normalizedKeyword) ||
                x.VenueId.ToString().Contains(normalizedKeyword));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLowerInvariant();

            if (normalizedStatus == "active")
            {
                query = query.Where(x => x.VenueStatus != "0");
            }
            else if (normalizedStatus == "inactive")
            {
                query = query.Where(x => x.VenueStatus == "0");
            }
        }

        return await query.OrderBy(x => x.VenueId).ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextVenueIdAsync(CancellationToken cancellationToken = default)
    {
        var maxId = await Context.Venues.MaxAsync(x => (int?)x.VenueId, cancellationToken) ?? 0;
        return maxId + 1;
    }
}
