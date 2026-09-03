using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoachesController : ControllerBase
{
    private readonly ICoachAppService _coachAppService;

    public CoachesController(ICoachAppService coachAppService)
    {
        _coachAppService = coachAppService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CoachDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CoachDto>>>> GetList(
        [FromQuery] string? keyword,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        CancellationToken cancellationToken)
    {
        var coaches = await _coachAppService.GetManagementListAsync(keyword, sortBy, sortDirection, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CoachDto>>.Success(coaches, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CoachDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CoachDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var coach = await _coachAppService.GetByIdAsync(id, cancellationToken);
        if (coach is null)
        {
            return NotFound(ApiResponse<object>.Failure("NOT_FOUND", $"未找到编号为 {id} 的教练。", HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<CoachDto>.Success(coach, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CoachDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CoachDto>>> Create(
        [FromBody] CreateCoachRequestDto request,
        CancellationToken cancellationToken)
    {
        var coach = await _coachAppService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = coach.CoachId }, ApiResponse<CoachDto>.Success(coach, HttpContext.TraceIdentifier, "教练创建成功"));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CoachDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CoachDto>>> Update(
        int id,
        [FromBody] UpdateCoachRequestDto request,
        CancellationToken cancellationToken)
    {
        var coach = await _coachAppService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CoachDto>.Success(coach, HttpContext.TraceIdentifier, "教练信息已更新"));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CoachDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CoachDto>>> Deactivate(int id, CancellationToken cancellationToken)
    {
        var coach = await _coachAppService.DeactivateAsync(id, cancellationToken);
        return Ok(ApiResponse<CoachDto>.Success(coach, HttpContext.TraceIdentifier, "教练账号已注销"));
    }
}
