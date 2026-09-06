using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;

namespace Application.Services;

public sealed class WaitingQueueAppService
    : IWaitingQueueAppService
{
    private readonly IMemberRepository _memberRepository;
    private readonly IGroupcourseRepository _groupcourseRepository;
    private readonly IWaitingQueueRepository _waitingQueueRepository;

    public WaitingQueueAppService(
        IMemberRepository memberRepository,
        IGroupcourseRepository groupcourseRepository,
        IWaitingQueueRepository waitingQueueRepository)
    {
        _memberRepository = memberRepository;
        _groupcourseRepository = groupcourseRepository;
        _waitingQueueRepository = waitingQueueRepository;
    }

    public async Task<(bool Success, WaitingQueueDto? Data, string Message)> JoinAsync(
        WaitingQueueRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(
            request.MemberId,
            cancellationToken);

        if (member is null)
        {
            return (false, null, "会员不存在");
        }

        var course = await _groupcourseRepository.GetByIdAsync(
            request.CourseId,
            cancellationToken);

        if (course is null)
        {
            return (false, null, "团课不存在");
        }

        var result = await _waitingQueueRepository.JoinAsync(
            request.MemberId,
            request.CourseId,
            cancellationToken);

        if (!result.Success)
        {
            return (false, null, result.Message);
        }

        var data = new WaitingQueueDto
        {
            QueueId = result.QueueId,
            MemberId = request.MemberId,
            CourseId = request.CourseId,
            CourseName = course.CourseName,
            Message = result.Message
        };

        return (true, data, result.Message);
    }

    public async Task<IReadOnlyList<WaitingQueueDto>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        var queues = await _waitingQueueRepository.GetByMemberIdAsync(
            memberId,
            cancellationToken);

        return queues
            .Select(x => new WaitingQueueDto
            {
                QueueId = x.QueueId,
                MemberId = x.MemberId,
                CourseId = x.CourseId,
                CourseName = x.Course.CourseName,
                EnqueueTime = x.EnqueueTime,
                QueueStatus = x.QueueStatus,
                Message = "候补中"
            })
            .ToList();
    }
}
