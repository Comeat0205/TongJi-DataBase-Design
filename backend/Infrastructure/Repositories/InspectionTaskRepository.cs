using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

    public Task<bool> VenueExistsAsync(int venueId, CancellationToken cancellationToken = default)
    {
        return Context.Venues.AnyAsync(venue => venue.VenueId == venueId, cancellationToken);
    }

    public Task<bool> EmployeeExistsAsync(int empId, CancellationToken cancellationToken = default)
    {
        return Context.Employees.AnyAsync(employee => employee.EmpId == empId, cancellationToken);
    }

    public async Task<int> GetNextIdAsync(CancellationToken cancellationToken = default)
    {
        var nextValue = await Context.Database
            .SqlQueryRaw<decimal>("SELECT SEQ_INSPECTIONTASK.NEXTVAL AS \"Value\" FROM DUAL")
            .SingleAsync(cancellationToken);

        return checked((int)nextValue);
    }
}
