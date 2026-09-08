using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class CoachScheduleRepository : Repository<CoachSchedule, int>, ICoachScheduleRepository
{
    public CoachScheduleRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<CoachSchedule>> GetByCoachIdAsync(int coachId, CancellationToken cancellationToken = default)
    {
        return await Context.CoachSchedules
            .AsNoTracking()
            .Where(x => x.CoachId == coachId)
            .OrderBy(x => x.ScheduleDate)
            .ThenBy(x => x.ScheduleStart)
            .ToListAsync(cancellationToken);
    }
}
