using Application.DTOs;

namespace Application.Interfaces;

public interface IMemberAppService
{
    Task<MemberDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MemberDto>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<MemberDto> UpdateAsync(int id, UpdateMemberRequestDto request, CancellationToken cancellationToken = default);
    Task<MemberDto> RegisterAsync(RegisterMemberRequestDto request, CancellationToken cancellationToken = default);
}
