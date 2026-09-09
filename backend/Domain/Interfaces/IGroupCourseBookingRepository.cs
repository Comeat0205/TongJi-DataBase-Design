using Domain.Entities;

namespace Domain.Interfaces;

public interface IGroupCourseBookingRepository : IRepository<GroupCourseBooking, int>
{
    Task<IReadOnlyList<GroupCourseBooking>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default);

    Task<GroupCourseBooking?> GetActiveByMemberAndCourseAsync(
        int memberId,
        int courseId,
        CancellationToken cancellationToken = default);

    Task<GroupCourseBooking?> GetActiveByMemberCourseAndDateAsync(
        int memberId,
        int courseId,
        DateTime courseDate,
        CancellationToken cancellationToken = default);

    Task<GroupCourseBooking?> GetDetailByIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<int> GetNextBookingIdAsync(CancellationToken cancellationToken = default);
}
