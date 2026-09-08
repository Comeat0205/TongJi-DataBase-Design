using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/inspection-tasks")]
public sealed class InspectionTasksController : ControllerBase
{
    private readonly IInspectionTaskAppService _inspectionTaskAppService;

    public InspectionTasksController(IInspectionTaskAppService inspectionTaskAppService)
    {
        _inspectionTaskAppService = inspectionTaskAppService;
    }

    [HttpGet("options")]
    [ProducesResponseType(typeof(ApiResponse<InspectionTaskOptionsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InspectionTaskOptionsDto>>> GetOptions(
        CancellationToken cancellationToken)
    {
        var options = await _inspectionTaskAppService.GetOptionsAsync(cancellationToken);
        return Ok(ApiResponse<InspectionTaskOptionsDto>.Success(
            options,
            HttpContext.TraceIdentifier));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<InspectionTaskDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InspectionTaskDto>>>> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var tasks = await _inspectionTaskAppService.GetPagedAsync(
            pageNumber,
            pageSize,
            status,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<InspectionTaskDto>>.Success(
            tasks,
            HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<InspectionTaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<InspectionTaskDto>>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var task = await _inspectionTaskAppService.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return NotFound(ApiResponse<object>.Failure(
                "NOT_FOUND",
                $"未找到编号为 {id} 的巡检任务。",
                HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<InspectionTaskDto>.Success(task, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InspectionTaskDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<InspectionTaskDto>>> Create(
        [FromBody] CreateInspectionTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _inspectionTaskAppService.CreateAsync(request, cancellationToken);
        var response = ApiResponse<InspectionTaskDto>.Success(
            task,
            HttpContext.TraceIdentifier,
            "巡检任务创建成功。");

        return CreatedAtAction(nameof(GetById), new { id = task.TaskId }, response);
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(ApiResponse<InspectionTaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<InspectionTaskDto>>> UpdateStatus(
        int id,
        [FromBody] UpdateInspectionTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _inspectionTaskAppService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse<InspectionTaskDto>.Success(
            task,
            HttpContext.TraceIdentifier,
            "巡检状态更新成功。"));
    }
}
