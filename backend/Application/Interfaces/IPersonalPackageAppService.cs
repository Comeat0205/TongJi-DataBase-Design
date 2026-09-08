using Application.DTOs;

namespace Application.Interfaces;

public interface IPersonalPackageAppService
{
    Task<IReadOnlyList<PersonalPackageDto>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalPackageProductDto>> GetProductsAsync(
        CancellationToken cancellationToken = default);

    Task<PersonalPackageDto> CreateAsync(
        CreatePersonalPackageRequestDto request,
        CancellationToken cancellationToken = default);
}
