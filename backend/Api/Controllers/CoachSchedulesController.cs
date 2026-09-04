using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoachSchedulesController : ControllerBase
{
    private readonly IScheduleAppService _scheduleAppService;

    public CoachSchedulesController(IScheduleAppService scheduleAppService)
    {
        _scheduleAppService = scheduleAppService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CoachScheduleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CoachScheduleDto>>>> GetByCoach(
        [FromQuery] int coachId,
        CancellationToken cancellationToken = default)
    {
        var schedules = await _scheduleAppService.GetCoachSchedulesAsync(coachId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CoachScheduleDto>>.Success(schedules, HttpContext.TraceIdentifier));
    }
}
