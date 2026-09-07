using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MemberSchedulesController : ControllerBase
{
    private readonly IScheduleAppService _scheduleAppService;

    public MemberSchedulesController(IScheduleAppService scheduleAppService)
    {
        _scheduleAppService = scheduleAppService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MemberScheduleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MemberScheduleDto>>>> GetByMember(
        [FromQuery] int memberId,
        CancellationToken cancellationToken = default)
    {
        var schedules = await _scheduleAppService.GetMemberSchedulesAsync(memberId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<MemberScheduleDto>>.Success(schedules, HttpContext.TraceIdentifier));
    }
}
