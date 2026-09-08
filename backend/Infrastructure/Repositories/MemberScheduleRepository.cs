using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class MemberScheduleRepository : Repository<MemberSchedule, int>, IMemberScheduleRepository
{
    public MemberScheduleRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<MemberSchedule>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default)
    {
        return await Context.MemberSchedules
            .AsNoTracking()
            .Where(x => x.MemberId == memberId)
            .OrderBy(x => x.ScheduleDate)
            .ThenBy(x => x.ScheduleStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<MemberSchedule?> GetBySourceTrackedAsync(
        string scheduleType,
        int sourceRecordId,
        CancellationToken cancellationToken = default)
    {
        return await Context.MemberSchedules
            .FirstOrDefaultAsync(
                x => x.ScheduleType == scheduleType && x.SourceRecordId == sourceRecordId,
                cancellationToken);
    }

    public async Task<int> GetNextScheduleIdAsync(CancellationToken cancellationToken = default)
    {
        var maxId = await Context.MemberSchedules.MaxAsync(x => (int?)x.ScheduleId, cancellationToken);
        return (maxId ?? 0) + 1;
    }
}
