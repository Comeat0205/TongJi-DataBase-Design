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

    public async Task<IReadOnlyList<Capacitylog>> GetByVenueAndDateAsync(
        int venueId,
        DateTime dayStart,
        DateTime dayEndExclusive,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(x => x.Venue)
            .Where(x => x.VenueId == venueId
                        && x.LogTimestamp != null
                        && x.LogTimestamp >= dayStart
                        && x.LogTimestamp < dayEndExclusive)
            .OrderBy(x => x.LogTimestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAtAsync(int venueId, DateTime timestamp, CancellationToken cancellationToken = default)
    {
        // Oracle DATE 精度到秒；对齐到分钟后做区间匹配，避免毫秒差异导致重复写入。
        // 使用 CountAsync 而非 AnyAsync，规避部分 Oracle EF 翻译 FALSE 的问题。
        var from = timestamp;
        var to = timestamp.AddMinutes(1);
        var count = await DbSet
            .AsNoTracking()
            .CountAsync(
                x => x.VenueId == venueId
                     && x.LogTimestamp != null
                     && x.LogTimestamp >= from
                     && x.LogTimestamp < to,
                cancellationToken);
        return count > 0;
    }

    public async Task<int> GetNextIdAsync(CancellationToken cancellationToken = default)
    {
        var maxId = await DbSet
            .AsNoTracking()
            .MaxAsync(x => (int?)x.CapacityLogId, cancellationToken);
        return (maxId ?? 0) + 1;
    }
}
