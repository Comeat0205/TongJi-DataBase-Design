using Application.DTOs;

namespace Application.Interfaces;

public interface IMemberAppService
{
    Task<MemberDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MemberDto>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}


