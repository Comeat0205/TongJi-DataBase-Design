using Application.DTOs;

namespace Application.Interfaces;

public interface IScheduleAppService
{
    Task<IReadOnlyList<MemberScheduleDto>> GetMemberSchedulesAsync(int memberId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CoachScheduleDto>> GetCoachSchedulesAsync(int coachId, CancellationToken cancellationToken = default);
}
