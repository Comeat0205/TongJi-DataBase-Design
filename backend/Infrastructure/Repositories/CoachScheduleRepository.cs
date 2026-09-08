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

    public async Task<CoachSchedule?> GetBySourceTrackedAsync(
        string scheduleType,
        int sourceRecordId,
        CancellationToken cancellationToken = default)
    {
        return await Context.CoachSchedules
            .FirstOrDefaultAsync(
                x => x.ScheduleType == scheduleType && x.SourceRecordId == sourceRecordId,
                cancellationToken);
    }

    public async Task<int> GetNextScheduleIdAsync(CancellationToken cancellationToken = default)
    {
        var maxId = await Context.CoachSchedules.MaxAsync(x => (int?)x.ScheduleId, cancellationToken);
        return (maxId ?? 0) + 1;
    }
}
