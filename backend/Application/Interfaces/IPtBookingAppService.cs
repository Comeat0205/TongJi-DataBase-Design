using Application.DTOs;

namespace Application.Interfaces;

public interface IPtBookingAppService
{
    Task<IReadOnlyList<PtBookingDto>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PtBookingDto>> GetPendingByCoachIdAsync(
        int coachId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PtBookingDto>> GetByCoachIdAsync(
        int coachId,
        CancellationToken cancellationToken = default);

    Task<PtBookingDto> BookAsync(
        CreatePtBookingRequestDto request,
        CancellationToken cancellationToken = default);

    Task CancelAsync(
        int bookingId,
        int memberId,
        CancellationToken cancellationToken = default);

    Task ConfirmAsync(
        int bookingId,
        ConfirmPtBookingRequestDto request,
        CancellationToken cancellationToken = default);

    Task ConsumeAsync(
        int bookingId,
        PtBookingCoachActionRequestDto request,
        CancellationToken cancellationToken = default);

    Task UndoConsumptionAsync(
        int bookingId,
        PtBookingCoachActionRequestDto request,
        CancellationToken cancellationToken = default);
}
