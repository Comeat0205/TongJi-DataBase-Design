using Domain.Entities;

namespace Domain.Interfaces;

public interface IMemberScheduleRepository : IRepository<MemberSchedule, int>
{
    Task<IReadOnlyList<MemberSchedule>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default);

    Task<MemberSchedule?> GetBySourceTrackedAsync(
        string scheduleType,
        int sourceRecordId,
        CancellationToken cancellationToken = default);

    Task<int> GetNextScheduleIdAsync(CancellationToken cancellationToken = default);
}
