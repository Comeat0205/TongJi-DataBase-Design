using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class CheckInOutRepository : Repository<Checkinout, int>, ICheckInOutRepository
{
    public CheckInOutRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Checkinout?> GetActiveCheckInAsync(int cardId, int venueId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.CardId == cardId
                && x.VenueId == venueId
                && x.CheckOutTime == null, cancellationToken);
    }

    public async Task<IReadOnlyList<Checkinout>> GetActiveCheckInsByVenueAsync(int venueId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(x => x.Card!)
                .ThenInclude(c => c.Member)
            .Where(x => x.VenueId == venueId && x.CheckOutTime == null)
            .OrderByDescending(x => x.CheckInTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<Checkinout?> GetWithDetailsAsync(int checkInOutId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.Card!)
                .ThenInclude(c => c.Member)
            .Include(x => x.Venue)
            .FirstOrDefaultAsync(x => x.CheckInOutId == checkInOutId, cancellationToken);
    }

    public async Task<IReadOnlyList<Checkinout>> GetPagedAsync(int venueId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(x => x.Card!)
                .ThenInclude(c => c.Member)
            .Include(x => x.Venue)
            .AsQueryable();

        if (venueId > 0)
        {
            query = query.Where(x => x.VenueId == venueId);
        }

        return await query
            .OrderByDescending(x => x.CheckInTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextIdAsync(CancellationToken cancellationToken = default)
    {
        var maxId = await DbSet
            .AsNoTracking()
            .MaxAsync(x => (int?)x.CheckInOutId, cancellationToken);
        return (maxId ?? 0) + 1;
    }
}
