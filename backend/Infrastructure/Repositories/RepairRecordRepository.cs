using System.Data;
using System.Globalization;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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

    public async Task<bool> EquipmentExistsAsync(int equipId, CancellationToken cancellationToken = default)
    {
        return await Context.Equipment
            .AsNoTracking()
            .Where(equipment => equipment.EquipId == equipId)
            .Select(_ => 1)
            .FirstOrDefaultAsync(cancellationToken) == 1;
    }

    public async Task<bool> EmployeeExistsAsync(int empId, CancellationToken cancellationToken = default)
    {
        return await Context.Employees
            .AsNoTracking()
            .Where(employee => employee.EmpId == empId)
            .Select(_ => 1)
            .FirstOrDefaultAsync(cancellationToken) == 1;
    }

    public async Task<int> GetNextIdAsync(CancellationToken cancellationToken = default)
    {
        var connection = Context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await Context.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT SEQ_REPAIRRECORD.NEXTVAL FROM DUAL";
            command.Transaction = Context.Database.CurrentTransaction?.GetDbTransaction();
            var nextValue = await command.ExecuteScalarAsync(cancellationToken)
                ?? throw new InvalidOperationException("未能获取报修记录序列值。");

            return checked(Convert.ToInt32(nextValue, CultureInfo.InvariantCulture));
        }
        finally
        {
            if (shouldClose)
            {
                await Context.Database.CloseConnectionAsync();
            }
        }
    }
}
