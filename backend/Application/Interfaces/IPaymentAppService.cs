using Application.DTOs;

namespace Application.Interfaces;

public interface IPaymentAppService
{
    Task<IReadOnlyList<PaymentOrderDto>> GetOrdersAsync(
        int? memberId,
        int? businessOrderId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VoucherDto>> GetVouchersAsync(
        int? memberId,
        string? voucherType,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VoucherDto>> GetAvailableVouchersAsync(
        int memberId,
        int? forOrderId,
        CancellationToken cancellationToken = default);

    Task<VoucherDto> IssueDiscountVoucherAsync(IssueDiscountVoucherRequestDto request, CancellationToken cancellationToken = default);

    Task<int> IssueDiscountVouchersToAllAsync(CancellationToken cancellationToken = default);

    Task<VoucherDto> IssueWelcomeVoucherAsync(int memberId, CancellationToken cancellationToken = default);

    Task<int> IssueBirthdayVouchersForTodayAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AtRiskMemberDto>> GetAtRiskMembersAsync(
        int inactiveDays,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PaymentOrderDto> CreateOrderAsync(CreatePaymentOrderRequestDto request, CancellationToken cancellationToken = default);

    Task<PaymentOrderDto?> UpdateOrderVoucherAsync(
        int orderId,
        UpdateOrderVoucherRequestDto request,
        CancellationToken cancellationToken = default);

    Task<PaymentOrderDto?> PayOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task<PaymentOrderDto?> CancelOrderAsync(int orderId, CancellationToken cancellationToken = default);
}
