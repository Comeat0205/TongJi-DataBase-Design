using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PersonalPackageRepository
    : Repository<Personalpackage, int>, IPersonalPackageRepository
{
    public PersonalPackageRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Personalpackage>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Personalpackages
            .AsNoTracking()
            .Include(x => x.Coach)
            .Include(x => x.PersonalCourse)
            .Where(x => x.MemberId == memberId)
            .OrderByDescending(x => x.ExpireDate)
            .ThenBy(x => x.PackageId)
            .ToListAsync(cancellationToken);
    }
}
