namespace Application.DTOs;

public sealed class PaymentOrderDto
{
    public int OrderId { get; init; }
    public int BusinessOrderId { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal DiscountValue { get; init; }
    public decimal PayableAmount { get; init; }
    public string? PaymentStatus { get; init; }
    public DateTime? CreateTime { get; init; }
    public DateTime? PaymentFinishTime { get; init; }
    public int? VoucherId { get; init; }
    public string? VoucherType { get; init; }
    public int? MemberId { get; init; }
    public int DetailCount { get; init; }
    public decimal? RefundAmount { get; init; }
    public bool? VoucherRestored { get; init; }
    public string? ActionMessage { get; init; }
}
