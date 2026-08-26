using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/pt-bookings")]
public sealed class PtBookingsController : ControllerBase
{
    private readonly IPtBookingAppService _ptBookingAppService;

    public PtBookingsController(IPtBookingAppService ptBookingAppService)
    {
        _ptBookingAppService = ptBookingAppService;
    }

    [HttpGet("/api/members/{memberId:int}/pt-bookings")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PtBookingDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PtBookingDto>>>> GetByMemberId(
        int memberId,
        CancellationToken cancellationToken)
    {
        var bookings = await _ptBookingAppService.GetByMemberIdAsync(memberId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PtBookingDto>>.Success(
            bookings,
            HttpContext.TraceIdentifier));
    }

    [HttpGet("/api/coaches/{coachId:int}/pt-bookings/pending")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PtBookingDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PtBookingDto>>>> GetPendingByCoachId(
        int coachId,
        CancellationToken cancellationToken)
    {
        var bookings = await _ptBookingAppService.GetPendingByCoachIdAsync(coachId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PtBookingDto>>.Success(
            bookings,
            HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PtBookingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PtBookingDto>>> Book(
        [FromBody] CreatePtBookingRequestDto request,
        CancellationToken cancellationToken)
    {
        var booking = await _ptBookingAppService.BookAsync(request, cancellationToken);
        return Ok(ApiResponse<PtBookingDto>.Success(
            booking,
            HttpContext.TraceIdentifier,
            "私教预约已提交，等待教练确认。"));
    }

    [HttpDelete("{bookingId:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(
        int bookingId,
        [FromQuery] int memberId,
        CancellationToken cancellationToken)
    {
        await _ptBookingAppService.CancelAsync(bookingId, memberId, cancellationToken);
        return Ok(ApiResponse<object>.Success(
            null,
            HttpContext.TraceIdentifier,
            "私教预约已取消。"));
    }

    [HttpPost("{bookingId:int}/confirm")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> Confirm(
        int bookingId,
        [FromBody] ConfirmPtBookingRequestDto request,
        CancellationToken cancellationToken)
    {
        await _ptBookingAppService.ConfirmAsync(bookingId, request, cancellationToken);
        var message = request.Accept ? "私教预约已确认并完成消课。" : "私教预约已拒绝。";
        return Ok(ApiResponse<object>.Success(null, HttpContext.TraceIdentifier, message));
    }
}
