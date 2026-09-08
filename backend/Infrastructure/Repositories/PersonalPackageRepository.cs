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

    public async Task<PersonalCourse?> GetCourseByIdAsync(
        int personalCourseId,
        CancellationToken cancellationToken = default)
    {
        return await Context.PersonalCourses
            .AsNoTracking()
            .Include(x => x.Coach)
            .FirstOrDefaultAsync(x => x.PersonalCourseId == personalCourseId, cancellationToken);
    }

    public async Task<Personalpackage?> GetDetailByIdAsync(
        int packageId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Personalpackages
            .AsNoTracking()
            .Include(x => x.Coach)
            .Include(x => x.PersonalCourse)
            .FirstOrDefaultAsync(x => x.PackageId == packageId, cancellationToken);
    }

    public async Task<int> GetNextPackageIdAsync(CancellationToken cancellationToken = default)
    {
        var maxId = await Context.Personalpackages.MaxAsync(x => (int?)x.PackageId, cancellationToken);
        return (maxId ?? 0) + 1;
    }
}
