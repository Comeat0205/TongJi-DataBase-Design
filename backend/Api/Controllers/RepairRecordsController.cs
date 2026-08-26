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
}
