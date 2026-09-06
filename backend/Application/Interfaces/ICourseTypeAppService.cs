using Application.DTOs;

namespace Application.Interfaces;

public interface ICourseTypeAppService
{
    Task<IReadOnlyList<CourseTypeDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<(bool Success, CourseTypeDto? Data, string Message)> CreateAsync(
        CourseTypeRequestDto request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, CourseTypeDto? Data, string Message)> UpdateAsync(
        int typeId,
        CourseTypeRequestDto request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> DeleteAsync(
        int typeId,
        CancellationToken cancellationToken = default);
}
