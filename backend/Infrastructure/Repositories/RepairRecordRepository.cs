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

    public async Task<Repairrecord?> GetDetailsByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await Context.Repairrecords
            .AsNoTracking()
            .Include(record => record.Equip)
            .Include(record => record.Emp)
            .SingleOrDefaultAsync(record => record.RecordId == id, cancellationToken);
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

    public Task<bool> EquipmentExistsAsync(int equipId, CancellationToken cancellationToken = default)
    {
        return Context.Equipment.AnyAsync(equipment => equipment.EquipId == equipId, cancellationToken);
    }

    public Task<bool> EmployeeExistsAsync(int empId, CancellationToken cancellationToken = default)
    {
        return Context.Employees.AnyAsync(employee => employee.EmpId == empId, cancellationToken);
    }

    public async Task<int> GetNextIdAsync(CancellationToken cancellationToken = default)
    {
        var nextValue = await Context.Database
            .SqlQueryRaw<decimal>("SELECT SEQ_REPAIRRECORD.NEXTVAL AS \"Value\" FROM DUAL")
            .SingleAsync(cancellationToken);

        return checked((int)nextValue);
    }
}
