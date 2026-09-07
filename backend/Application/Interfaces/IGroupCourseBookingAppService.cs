using Application.DTOs;

namespace Application.Interfaces;

public interface IGroupCourseBookingAppService
{
    Task<(bool Success, GroupCourseBookingDto? Data, string Message)> BookAsync(
        GroupCourseBookingRequestDto request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> CancelAsync(
        int memberId,
        int courseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupCourseBookingDto>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default);
}