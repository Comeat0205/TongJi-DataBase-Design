using Application.DTOs;

namespace Application.Interfaces;

public interface IGroupCourseBookingAppService
{
    Task<(bool Success, GroupCourseBookingDto? Data, string Message)> BookAsync(
        GroupCourseBookingRequestDto request,
        CancellationToken cancellationToken = default);
}
