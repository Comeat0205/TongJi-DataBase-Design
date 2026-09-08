namespace Application.DTOs;

public sealed class CreatePtBookingRequestDto
{
    public int MemberId { get; init; }
    public int PackageId { get; init; }
    public DateTime SessionTime { get; init; }
}
