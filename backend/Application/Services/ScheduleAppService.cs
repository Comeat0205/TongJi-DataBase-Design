using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public sealed class ScheduleAppService : IScheduleAppService
{
    private readonly IMemberScheduleRepository _memberScheduleRepository;
    private readonly ICoachScheduleRepository _coachScheduleRepository;

    public ScheduleAppService(
        IMemberScheduleRepository memberScheduleRepository,
        ICoachScheduleRepository coachScheduleRepository)
    {
        _memberScheduleRepository = memberScheduleRepository;
        _coachScheduleRepository = coachScheduleRepository;
    }

    public async Task<IReadOnlyList<MemberScheduleDto>> GetMemberSchedulesAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var schedules = await _memberScheduleRepository.GetByMemberIdAsync(memberId, cancellationToken);
        return schedules.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<CoachScheduleDto>> GetCoachSchedulesAsync(int coachId, CancellationToken cancellationToken = default)
    {
        var schedules = await _coachScheduleRepository.GetByCoachIdAsync(coachId, cancellationToken);
        return schedules.Select(MapToDto).ToList();
    }

    private static MemberScheduleDto MapToDto(MemberSchedule schedule)
    {
        return new MemberScheduleDto
        {
            ScheduleId = schedule.ScheduleId,
            MemberId = schedule.MemberId,
            ScheduleStart = schedule.ScheduleStart,
            ScheduleDate = schedule.ScheduleDate,
            ScheduleEnd = schedule.ScheduleEnd,
            ScheduleType = schedule.ScheduleType,
            SourceRecordId = schedule.SourceRecordId,
            Status = schedule.Status
        };
    }

    private static CoachScheduleDto MapToDto(CoachSchedule schedule)
    {
        return new CoachScheduleDto
        {
            ScheduleId = schedule.ScheduleId,
            CoachId = schedule.CoachId,
            ScheduleStart = schedule.ScheduleStart,
            ScheduleEnd = schedule.ScheduleEnd,
            ScheduleDate = schedule.ScheduleDate,
            ScheduleType = schedule.ScheduleType,
            SourceRecordId = schedule.SourceRecordId,
            Status = schedule.Status
        };
    }
}
