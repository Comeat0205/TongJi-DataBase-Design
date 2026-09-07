using Domain.Entities;

namespace Domain.Interfaces;

public interface IPersonalPackageRepository : IRepository<Personalpackage, int>
{
    Task<IReadOnlyList<Personalpackage>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default);
}
