using Domain.Entities;

namespace Domain.Interfaces;

public interface ICoachScheduleRepository : IRepository<CoachSchedule, int>
{
    Task<IReadOnlyList<CoachSchedule>> GetByCoachIdAsync(int coachId, CancellationToken cancellationToken = default);

    Task<CoachSchedule?> GetBySourceTrackedAsync(
        string scheduleType,
        int sourceRecordId,
        CancellationToken cancellationToken = default);

    Task<int> GetNextScheduleIdAsync(CancellationToken cancellationToken = default);
}
