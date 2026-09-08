using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AbsenceRecordsController : ControllerBase
{
    private readonly IAbsenceRecordAppService _absenceRecordAppService;

    public AbsenceRecordsController(
        IAbsenceRecordAppService absenceRecordAppService)
    {
        _absenceRecordAppService = absenceRecordAppService;
    }

    [HttpGet("member/{memberId:int}")]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<AbsenceRecordDto>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AbsenceRecordDto>>>> GetByMemberId(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        var records = await _absenceRecordAppService.GetByMemberIdAsync(
            memberId,
            cancellationToken);

        return Ok(
            ApiResponse<IReadOnlyList<AbsenceRecordDto>>.Success(
                records,
                HttpContext.TraceIdentifier,
                "查询成功"));
    }
}
