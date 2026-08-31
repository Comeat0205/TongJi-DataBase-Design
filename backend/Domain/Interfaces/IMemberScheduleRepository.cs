using Domain.Entities;

namespace Domain.Interfaces;

public interface IMemberScheduleRepository : IRepository<MemberSchedule, int>
{
    Task<IReadOnlyList<MemberSchedule>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default);
}
