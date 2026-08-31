using System.Data;
using System.Globalization;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Repositories;

public sealed class InspectionTaskRepository : Repository<Inspectiontask, int>, IInspectionTaskRepository
{
    public InspectionTaskRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Inspectiontask?> GetDetailsByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await Context.Inspectiontasks
            .AsNoTracking()
            .Include(task => task.Emp)
            .SingleOrDefaultAsync(task => task.TaskId == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Inspectiontask>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Inspectiontasks
            .AsNoTracking()
            .Include(task => task.Emp)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(task => task.Status == status);
        }

        return await query
            .OrderByDescending(task => task.TaskTime)
            .ThenByDescending(task => task.TaskId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, string>> GetVenueNamesAsync(
        IEnumerable<int> venueIds,
        CancellationToken cancellationToken = default)
    {
        var ids = venueIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<int, string>();
        }

        return await Context.Venues
            .AsNoTracking()
            .Where(venue => ids.Contains(venue.VenueId))
            .ToDictionaryAsync(venue => venue.VenueId, venue => venue.VenueName, cancellationToken);
    }

    public async Task<bool> VenueExistsAsync(int venueId, CancellationToken cancellationToken = default)
    {
        return await Context.Venues
            .AsNoTracking()
            .Where(venue => venue.VenueId == venueId)
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
            command.CommandText = "SELECT SEQ_INSPECTIONTASK.NEXTVAL FROM DUAL";
            command.Transaction = Context.Database.CurrentTransaction?.GetDbTransaction();
            var nextValue = await command.ExecuteScalarAsync(cancellationToken)
                ?? throw new InvalidOperationException("未能获取巡检任务序列值。");

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
