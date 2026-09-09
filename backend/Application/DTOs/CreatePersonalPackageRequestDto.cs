namespace Application.DTOs;

public sealed class CreatePersonalPackageRequestDto
{
    public int MemberId { get; init; }
    public int PriceId { get; init; }
}
