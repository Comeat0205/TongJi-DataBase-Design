using Domain.Entities;

namespace Domain.Interfaces;

public interface IVoucherRepository : IRepository<Voucher, int>
{
    Task<IReadOnlyList<Voucher>> GetListAsync(int? memberId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Voucher>> GetAvailableAsync(
        int memberId,
        int? excludePendingOrderId,
        CancellationToken cancellationToken = default);

    Task<Voucher?> GetByIdTrackedAsync(int voucherId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(Member Member, DateTime? LastCheckInTime, int UnusedVoucherCount)>> GetAtRiskMembersAsync(
        int inactiveDays,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
