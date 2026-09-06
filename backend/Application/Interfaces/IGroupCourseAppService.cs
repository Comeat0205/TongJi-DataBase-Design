using Application.DTOs;

namespace Application.Interfaces;

public interface IGroupCourseAppService
{
    Task<IReadOnlyList<GroupCourseDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<(bool Success, GroupCourseDto? Data, string Message)> CreateAsync(
        GroupCourseRequestDto request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, GroupCourseDto? Data, string Message)> UpdateAsync(
        int courseId,
        GroupCourseRequestDto request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> DeleteAsync(
        int courseId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> CheckScheduleConflictAsync(
        GroupCourseScheduleConflictRequestDto request,
        CancellationToken cancellationToken = default);
}