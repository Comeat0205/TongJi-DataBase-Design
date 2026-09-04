using Application.DTOs;

namespace Application.Interfaces;

public interface IMemberAppService
{
    Task<MemberDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MemberDto>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    // feature/member-template  会员样板模块
    Task<MemberDto> UpdateAsync(int id, UpdateMemberRequestDto request, CancellationToken cancellationToken = default);
    Task<MemberDto> RegisterAsync(RegisterMemberRequestDto request, CancellationToken cancellationToken = default);

    // feature/basic-info  基本信息模块
    Task<IReadOnlyList<MemberManagementListItemDto>> GetManagementListAsync(string? keyword, string? sortBy, string? sortDirection, CancellationToken cancellationToken = default);
    Task ValidateRegistrationAccountAsync(ValidateMemberRegistrationAccountRequestDto request, CancellationToken cancellationToken = default);
    Task<MemberDto> CancelAsync(int id, CancellationToken cancellationToken = default);
}
