using Domain.Entities;

namespace Domain.Interfaces;

public interface IAbsenceRecordRepository
{
    Task<IReadOnlyList<AbsenceRecord>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default);
}
