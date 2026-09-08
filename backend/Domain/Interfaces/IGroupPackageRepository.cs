using Domain.Entities;

namespace Domain.Interfaces;

public interface IGroupPackageRepository : IRepository<GroupPackage, int>
{
    Task<IReadOnlyList<GroupPackage>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default);

    Task<GroupPackage?> GetDetailByIdAsync(int packageId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupPackage>> GetUsableByMemberAndTypeAsync(
        int memberId,
        int typeId,
        CancellationToken cancellationToken = default);

    Task<int> GetNextPackageIdAsync(CancellationToken cancellationToken = default);
}
