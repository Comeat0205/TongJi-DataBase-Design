namespace Application.DTOs;

public sealed class VoucherDto
{
    public int VoucherId { get; init; }
    public int MemberId { get; init; }
    public string VoucherType { get; init; } = string.Empty;
    public decimal DiscountValue { get; init; }
    public DateTime ValidUntil { get; init; }
    public string? Status { get; init; }
    public string StatusText { get; init; } = string.Empty;
    public bool IsExpired { get; init; }
}
