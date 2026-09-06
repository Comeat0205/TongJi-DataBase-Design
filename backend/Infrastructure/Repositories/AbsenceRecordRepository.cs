using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class AbsenceRecordRepository
    : Repository<AbsenceRecord, int>, IAbsenceRecordRepository
{
    public AbsenceRecordRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<AbsenceRecord>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        return await Context.AbsenceRecords
            .Include(x => x.Booking)
                .ThenInclude(x => x.Course)
            .Where(x => x.MemberId == memberId)
            .OrderByDescending(x => x.CourseDate)
            .ThenByDescending(x => x.AbsenceTime)
            .ToListAsync(cancellationToken);
    }
}
