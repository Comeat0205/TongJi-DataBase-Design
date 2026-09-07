using Application.DTOs;

namespace Application.Interfaces;

public interface IWaitingQueueAppService
{
    Task<(bool Success, WaitingQueueDto? Data, string Message)> JoinAsync(
        WaitingQueueRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WaitingQueueDto>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default);
}
