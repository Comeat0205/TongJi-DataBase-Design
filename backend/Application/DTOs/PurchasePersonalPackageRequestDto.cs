namespace Application.DTOs;

public sealed class PurchasePersonalPackageRequestDto
{
    public int MemberId { get; init; }
    public int PriceId { get; init; }
    public int? VoucherId { get; init; }
}
