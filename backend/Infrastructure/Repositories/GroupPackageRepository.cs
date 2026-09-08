using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class GroupPackageRepository : Repository<GroupPackage, int>, IGroupPackageRepository
{
    public GroupPackageRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<GroupPackage>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        return await Context.GroupPackages
            .AsNoTracking()
            .Include(x => x.Course)
                .ThenInclude(c => c.Type)
            .Include(x => x.Course)
                .ThenInclude(c => c.Coach)
            .Where(x => x.MemberId == memberId)
            .OrderByDescending(x => x.PackageId)
            .ToListAsync(cancellationToken);
    }

    public async Task<GroupPackage?> GetDetailByIdAsync(
        int packageId,
        CancellationToken cancellationToken = default)
    {
        return await Context.GroupPackages
            .Include(x => x.Course)
                .ThenInclude(c => c.Type)
            .FirstOrDefaultAsync(x => x.PackageId == packageId, cancellationToken);
    }

    public async Task<IReadOnlyList<GroupPackage>> GetUsableByMemberAndTypeAsync(
        int memberId,
        int typeId,
        CancellationToken cancellationToken = default)
    {
        return await Context.GroupPackages
            .Include(x => x.Course)
            .Where(x => x.MemberId == memberId
                && x.PackageStatus == "1"
                && x.RemainingCount > 0
                && x.Course.TypeId == typeId)
            .OrderBy(x => x.PackageId)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextPackageIdAsync(CancellationToken cancellationToken = default)
    {
        var max = await Context.GroupPackages.MaxAsync(x => (int?)x.PackageId, cancellationToken) ?? 0;
        return max + 1;
    }
}
