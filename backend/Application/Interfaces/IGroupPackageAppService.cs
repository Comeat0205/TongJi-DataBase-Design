using Application.DTOs;

namespace Application.Interfaces;

public interface IGroupPackageAppService
{
    Task<IReadOnlyList<GroupPackageDto>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupPackageProductDto>> GetProductsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupPackageProductDto>> GetManageProductsAsync(CancellationToken cancellationToken = default);

    Task<GroupPackageProductDto> CreateProductAsync(
        CreateGroupPackageProductRequestDto request,
        CancellationToken cancellationToken = default);

    Task<GroupPackageProductDto> UpdateProductAsync(
        int priceId,
        UpdateGroupPackageProductRequestDto request,
        CancellationToken cancellationToken = default);

    Task<GroupPackageDto> IssueAsync(
        IssueGroupPackageRequestDto request,
        CancellationToken cancellationToken = default);
}
