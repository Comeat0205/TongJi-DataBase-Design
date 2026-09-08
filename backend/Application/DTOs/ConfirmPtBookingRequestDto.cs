namespace Application.DTOs;

public sealed class ConfirmPtBookingRequestDto
{
    public int CoachId { get; init; }
    public bool Accept { get; init; }
}
