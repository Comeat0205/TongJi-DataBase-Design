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

    public async Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Venues
            .AsNoTracking()
            .OrderBy(x => x.VenueId)
            .ToListAsync(cancellationToken);
    }
}
