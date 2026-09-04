using Application.DTOs;

namespace Application.Interfaces;

public interface ICoachAppService
{
    Task<IReadOnlyList<CoachDto>> GetManagementListAsync(string? keyword, string? sortBy, string? sortDirection, CancellationToken cancellationToken = default);
    Task<CoachDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CoachDto> CreateAsync(CreateCoachRequestDto request, CancellationToken cancellationToken = default);
    Task<CoachDto> UpdateAsync(int id, UpdateCoachRequestDto request, CancellationToken cancellationToken = default);
    Task<CoachDto> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
