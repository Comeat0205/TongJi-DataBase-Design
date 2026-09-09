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

    /// <summary>
    /// 教练在时段内是否已有「已确认且未取消」的私教预约（可排除当前预约）。
    /// </summary>
    Task<bool> HasConfirmedSessionOverlapAsync(
        int coachId,
        DateTime sessionStart,
        DateTime sessionEnd,
        int? excludeBookingId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 教练在时段内待确认的私教预约（跟踪实体，含课包，用于确认后自动驳回）。
    /// </summary>
    Task<IReadOnlyList<Ptbooking>> GetPendingOverlappingTrackedAsync(
        int coachId,
        DateTime sessionStart,
        DateTime sessionEnd,
        int excludeBookingId,
        CancellationToken cancellationToken = default);

    Task<int> BookAsync(
        int memberId,
        int packageId,
        DateTime sessionTime,
        CancellationToken cancellationToken = default);
}
