using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class GroupcourseRepository : Repository<Groupcourse, int>, IGroupcourseRepository
{
    public GroupcourseRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<Groupcourse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(c => c.Coach)
            .Include(c => c.Type)
            .Include(c => c.TimeSlot)
                .ThenInclude(t => t.TimeSlotInstances)
            .OrderBy(c => c.CourseId)
            .ToListAsync(cancellationToken);
    }
}