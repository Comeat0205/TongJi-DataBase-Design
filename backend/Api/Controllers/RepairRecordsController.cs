using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/repair-records")]
public sealed class RepairRecordsController : ControllerBase
{
    private readonly IRepairRecordAppService _repairRecordAppService;

    public RepairRecordsController(IRepairRecordAppService repairRecordAppService)
    {
        _repairRecordAppService = repairRecordAppService;
    }

    [HttpGet("options")]
    [ProducesResponseType(typeof(ApiResponse<RepairRecordOptionsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RepairRecordOptionsDto>>> GetOptions(
        CancellationToken cancellationToken)
    {
        var options = await _repairRecordAppService.GetOptionsAsync(cancellationToken);
        return Ok(ApiResponse<RepairRecordOptionsDto>.Success(
            options,
            HttpContext.TraceIdentifier));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RepairRecordDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RepairRecordDto>>>> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var records = await _repairRecordAppService.GetPagedAsync(
            pageNumber,
            pageSize,
            status,
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<RepairRecordDto>>.Success(
            records,
            HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<RepairRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RepairRecordDto>>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var record = await _repairRecordAppService.GetByIdAsync(id, cancellationToken);
        if (record is null)
        {
            return NotFound(ApiResponse<object>.Failure(
                "NOT_FOUND",
                $"未找到编号为 {id} 的报修记录。",
                HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<RepairRecordDto>.Success(record, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RepairRecordDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RepairRecordDto>>> Create(
        [FromBody] CreateRepairRecordRequest request,
        CancellationToken cancellationToken)
    {
        var record = await _repairRecordAppService.CreateAsync(request, cancellationToken);
        var response = ApiResponse<RepairRecordDto>.Success(
            record,
            HttpContext.TraceIdentifier,
            "报修记录创建成功。");

        return CreatedAtAction(nameof(GetById), new { id = record.RecordId }, response);
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(ApiResponse<RepairRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RepairRecordDto>>> UpdateStatus(
        int id,
        [FromBody] UpdateRepairRecordStatusRequest request,
        CancellationToken cancellationToken)
    {
        var record = await _repairRecordAppService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse<RepairRecordDto>.Success(
            record,
            HttpContext.TraceIdentifier,
            "报修状态更新成功。"));
    }
}
