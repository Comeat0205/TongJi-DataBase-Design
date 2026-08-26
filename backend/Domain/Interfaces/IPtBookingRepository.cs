using Domain.Entities;

namespace Domain.Interfaces;

public interface IPtBookingRepository : IRepository<Ptbooking, int>
{
    Task<IReadOnlyList<Ptbooking>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ptbooking>> GetPendingByCoachIdAsync(
        int coachId,
        CancellationToken cancellationToken = default);

    Task<Ptbooking?> GetWithPackageAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<int> BookAsync(
        int memberId,
        int packageId,
        DateTime sessionTime,
        CancellationToken cancellationToken = default);
}
