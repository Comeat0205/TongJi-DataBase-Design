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
        // Oracle 库时区为 UTC（dbtimezone=+00:00），比较基准用 UtcNow 对齐存储值。
        var now = DateTime.UtcNow;
        return schedules.Select(s => MapToDto(s, now)).ToList();
    }

    public async Task<IReadOnlyList<CoachScheduleDto>> GetCoachSchedulesAsync(int coachId, CancellationToken cancellationToken = default)
    {
        var schedules = await _coachScheduleRepository.GetByCoachIdAsync(coachId, cancellationToken);
        var conflictIds = FindConflictIds(schedules);
        return schedules.Select(s => MapToDto(s, conflictIds.Contains(s.ScheduleId))).ToList();
    }

    // 功能点 #11：未来 2 小时内开课、且状态为待上（0）的日程标记为「即将开课」。
    private static MemberScheduleDto MapToDto(MemberSchedule schedule, DateTime now)
    {
        var isUpcoming = schedule.ScheduleStart >= now
            && schedule.ScheduleStart <= now.AddHours(2)
            && schedule.Status == "0";

        return new MemberScheduleDto
        {
            ScheduleId = schedule.ScheduleId,
            MemberId = schedule.MemberId,
            ScheduleStart = schedule.ScheduleStart,
            ScheduleDate = schedule.ScheduleDate,
            ScheduleEnd = schedule.ScheduleEnd,
            ScheduleType = schedule.ScheduleType,
            SourceRecordId = schedule.SourceRecordId,
            Status = schedule.Status,
            IsUpcoming = isUpcoming
        };
    }

    // 功能点 #13：教练自身日程时间重叠（start < 另一条 end 且 end > 另一条 start）。
    private static HashSet<int> FindConflictIds(IReadOnlyList<CoachSchedule> schedules)
    {
        var conflicts = new HashSet<int>();
        for (var i = 0; i < schedules.Count; i++)
        {
            for (var j = i + 1; j < schedules.Count; j++)
            {
                var a = schedules[i];
                var b = schedules[j];
                if (a.ScheduleStart < b.ScheduleEnd && a.ScheduleEnd > b.ScheduleStart)
                {
                    conflicts.Add(a.ScheduleId);
                    conflicts.Add(b.ScheduleId);
                }
            }
        }

        return conflicts;
    }

    private static CoachScheduleDto MapToDto(CoachSchedule schedule, bool isConflict)
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
            Status = schedule.Status,
            IsConflict = isConflict
        };
    }
}
