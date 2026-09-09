using Domain.Entities;

namespace Domain.Interfaces;

public interface IPersonalPackageRepository : IRepository<Personalpackage, int>
{
    Task<IReadOnlyList<Personalpackage>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default);

    Task<PersonalCourse?> GetCourseByIdAsync(
        int personalCourseId,
        CancellationToken cancellationToken = default);

    Task<Personalpackage?> GetDetailByIdAsync(
        int packageId,
        CancellationToken cancellationToken = default);

    Task<int> GetNextPackageIdAsync(CancellationToken cancellationToken = default);
}
