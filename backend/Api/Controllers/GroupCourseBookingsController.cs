using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupCourseBookingsController : ControllerBase
{
    private readonly IGroupCourseBookingAppService _bookingAppService;

    public GroupCourseBookingsController(
        IGroupCourseBookingAppService bookingAppService)
    {
        _bookingAppService = bookingAppService;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ApiResponse<GroupCourseBookingDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<GroupCourseBookingDto>>> Book(
        [FromBody] GroupCourseBookingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _bookingAppService.BookAsync(
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(
                ApiResponse<object>.Failure(
                    "BOOKING_FAILED",
                    result.Message,
                    HttpContext.TraceIdentifier));
        }

        return Ok(
            ApiResponse<GroupCourseBookingDto>.Success(
                result.Data!,
                HttpContext.TraceIdentifier,
                result.Message));
    }
}
