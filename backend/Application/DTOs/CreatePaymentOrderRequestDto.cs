namespace Application.DTOs;

public sealed class CreatePaymentOrderRequestDto
{
    public int MemberId { get; set; }
    public decimal TotalAmount { get; set; } = 199m;
    public int? VoucherId { get; set; }
}
