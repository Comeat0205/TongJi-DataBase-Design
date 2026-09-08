using Domain.Entities;

namespace Domain.Interfaces;

public interface IWaitingQueueRepository : IRepository<WaitingQueue, int>
{
    Task<(bool Success, int QueueId, string Message)> JoinAsync(
        int memberId,
        int courseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WaitingQueue>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default);

    Task<WaitingQueue?> GetEarliestWaitingAsync(
        int courseId,
        CancellationToken cancellationToken = default);
}
