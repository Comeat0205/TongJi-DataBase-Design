using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class CapacityMovementRepository : Repository<CapacityMovement, int>, ICapacityMovementRepository
{
    private static int _tableReady;

    public CapacityMovementRepository(AppDbContext context) : base(context)
    {
    }

    public async Task EnsureTableAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _tableReady, 1, 0) == 1)
        {
            return;
        }

        // Oracle：表已存在时 ORA-00955，忽略即可。
        const string sql = """
            BEGIN
              EXECUTE IMMEDIATE '
                CREATE TABLE CAPACITYMOVEMENT (
                  MOVEMENT_ID NUMBER(10) PRIMARY KEY,
                  VENUE_ID NUMBER(10) NOT NULL,
                  MEMBER_ID NUMBER(10) NOT NULL,
                  EVENT_TIME DATE NOT NULL,
                  EVENT_TYPE VARCHAR2(1) NOT NULL,
                  RECORDED_COUNT NUMBER(10) NOT NULL,
                  OCCUPANCY_RATE NUMBER(5,2),
                  CHECK_IN_OUT_ID NUMBER(10)
                )';
            EXCEPTION
              WHEN OTHERS THEN
                IF SQLCODE != -955 THEN RAISE; END IF;
            END;
            """;

        try
        {
            await Context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch
        {
            Interlocked.Exchange(ref _tableReady, 0);
            throw;
        }
    }

    public async Task<int> GetNextIdAsync(CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);
        var maxId = await DbSet
            .AsNoTracking()
            .MaxAsync(x => (int?)x.MovementId, cancellationToken);
        return (maxId ?? 0) + 1;
    }

    public async Task<IReadOnlyList<CapacityMovement>> GetByVenueAndDateAsync(
        int venueId,
        DateTime dayStart,
        DateTime dayEndExclusive,
        CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);
        return await DbSet
            .AsNoTracking()
            .Include(x => x.Venue)
            .Where(x => x.VenueId == venueId
                        && x.EventTime >= dayStart
                        && x.EventTime < dayEndExclusive)
            .OrderByDescending(x => x.EventTime)
            .ThenByDescending(x => x.MovementId)
            .ToListAsync(cancellationToken);
    }
}
