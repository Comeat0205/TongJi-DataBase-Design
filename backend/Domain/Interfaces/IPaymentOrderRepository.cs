using Domain.Entities;

namespace Domain.Interfaces;

public interface IPaymentOrderRepository : IRepository<PaymentOrder, int>
{
    Task<IReadOnlyList<PaymentOrder>> GetListAsync(
        int? memberId,
        int? businessOrderId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PaymentOrder?> GetByIdWithDetailsAsync(int orderId, CancellationToken cancellationToken = default);

    Task<int> GetNextOrderIdAsync(CancellationToken cancellationToken = default);

    Task<int> GetNextBusinessOrderIdAsync(CancellationToken cancellationToken = default);

    Task<int> GetNextDetailIdAsync(CancellationToken cancellationToken = default);

    Task<int?> GetDefaultPriceIdAsync(CancellationToken cancellationToken = default);
}
