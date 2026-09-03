using Domain.Entities;

namespace Domain.Interfaces;

public interface IGroupCourseBookingRepository : IRepository<GroupCourseBooking, int>
{
    Task<bool> ExistsAsync(
        int memberId,
        int courseId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, int BookingId, string Message)> BookAsync(
        int memberId,
        int courseId,
        CancellationToken cancellationToken = default);
}