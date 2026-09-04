using Application.DTOs;

namespace Application.Interfaces;

public interface IPersonalPackageAppService
{
    Task<IReadOnlyList<PersonalPackageDto>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default);
}
