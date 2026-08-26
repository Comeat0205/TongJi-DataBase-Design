using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class RepairRecordRepository : Repository<Repairrecord, int>, IRepairRecordRepository
{
    public RepairRecordRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Repairrecord>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Repairrecords
            .AsNoTracking()
            .Include(record => record.Equip)
            .Include(record => record.Emp)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(record => record.Status == status);
        }

        return await query
            .OrderByDescending(record => record.ReportTime ?? DateTime.MinValue)
            .ThenByDescending(record => record.RecordId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
