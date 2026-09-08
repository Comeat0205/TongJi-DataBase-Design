using Domain.Entities;

namespace Domain.Interfaces;

public sealed record PtScheduleDetail(
    int PtBookingId,
    int MemberId,
    string MemberName,
    string CourseName,
    int CoachId,
    string CoachName);

public interface IPtBookingRepository : IRepository<Ptbooking, int>
{
    Task<IReadOnlyList<Ptbooking>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ptbooking>> GetPendingByCoachIdAsync(
        int coachId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ptbooking>> GetByCoachIdAsync(
        int coachId,
        CancellationToken cancellationToken = default);

    Task<Ptbooking?> GetWithPackageAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, PtScheduleDetail>> GetScheduleDetailsByIdsAsync(
        IReadOnlyCollection<int> bookingIds,
        CancellationToken cancellationToken = default);

    Task<int> BookAsync(
        int memberId,
        int packageId,
        DateTime sessionTime,
        CancellationToken cancellationToken = default);
}
