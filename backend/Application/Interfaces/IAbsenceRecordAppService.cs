using Application.DTOs;

namespace Application.Interfaces;

public interface IAbsenceRecordAppService
{
    Task<IReadOnlyList<AbsenceRecordDto>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default);
}
