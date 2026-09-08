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

    public async Task<Checkinout?> GetActiveCheckInByCardAsync(int cardId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.Venue)
            .FirstOrDefaultAsync(x => x.CardId == cardId && x.CheckOutTime == null, cancellationToken);
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

    public async Task<MemberBenefitCard?> GetCardWithDetailsAsync(int cardId, CancellationToken cancellationToken = default)
    {
        return await Context.MemberBenefitCards
            .Include(c => c.CountCardExtension)
            .Include(c => c.TimeCardExtension)
            .Include(c => c.Member)
            .FirstOrDefaultAsync(c => c.CardId == cardId, cancellationToken);
    }

    public async Task<int> GetTodayCheckInCountAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        return await DbSet
            .AsNoTracking()
            .CountAsync(x => x.CheckInTime >= today && x.CheckInTime < today.AddDays(1), cancellationToken);
    }

    public async Task<int> GetTotalActiveCountAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .CountAsync(x => x.CheckOutTime == null, cancellationToken);
    }

    public async Task<string> ExecuteAutoCheckoutAsync(CancellationToken cancellationToken = default)
    {
        var conn = Context.Database.GetDbConnection();
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_auto_checkout";
        cmd.CommandType = System.Data.CommandType.StoredProcedure;

        var pResult = cmd.CreateParameter();
        pResult.ParameterName = "p_result";
        pResult.Direction = System.Data.ParameterDirection.Output;
        cmd.Parameters.Add(pResult);

        var pMessage = cmd.CreateParameter();
        pMessage.ParameterName = "p_message";
        pMessage.Direction = System.Data.ParameterDirection.Output;
        pMessage.Size = 4000;
        cmd.Parameters.Add(pMessage);

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        return pMessage.Value?.ToString() ?? "自动签退完成";
    }
}
