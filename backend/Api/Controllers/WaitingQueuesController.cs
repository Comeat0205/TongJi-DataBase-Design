using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WaitingQueuesController : ControllerBase
{
    private readonly IWaitingQueueAppService _waitingQueueAppService;

    public WaitingQueuesController(
        IWaitingQueueAppService waitingQueueAppService)
    {
        _waitingQueueAppService = waitingQueueAppService;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ApiResponse<WaitingQueueDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<WaitingQueueDto>>> Join(
        [FromBody] WaitingQueueRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _waitingQueueAppService.JoinAsync(
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(
                ApiResponse<object>.Failure(
                    "WAITING_QUEUE_FAILED",
                    result.Message,
                    HttpContext.TraceIdentifier));
        }

        return Ok(
            ApiResponse<WaitingQueueDto>.Success(
                result.Data!,
                HttpContext.TraceIdentifier,
                result.Message));
    }

    [HttpGet("member/{memberId:int}")]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<WaitingQueueDto>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WaitingQueueDto>>>> GetByMemberId(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        var queues = await _waitingQueueAppService.GetByMemberIdAsync(
            memberId,
            cancellationToken);

        return Ok(
            ApiResponse<IReadOnlyList<WaitingQueueDto>>.Success(
                queues,
                HttpContext.TraceIdentifier,
                "查询成功"));
    }
}
