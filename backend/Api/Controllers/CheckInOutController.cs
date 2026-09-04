using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckInOutController : ControllerBase
{
    private readonly ICheckInOutAppService _svc;

    public CheckInOutController(ICheckInOutAppService svc) => _svc = svc;

    // 入场
    [HttpPost("check-in")]
    public async Task<ActionResult<ApiResponse<CheckInResultDto>>> CheckIn(
        [FromBody] CheckInRequestDto req, CancellationToken ct)
    {
        try
        {
            var result = await _svc.CheckInAsync(req, ct);
            return Ok(ApiResponse<CheckInResultDto>.Success(result, HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Failure("CHECK_IN_FAILED", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    // 退场
    [HttpPost("{id:int}/check-out")]
    public async Task<ActionResult<ApiResponse<CheckInOutDto>>> CheckOut(int id, CancellationToken ct)
    {
        try
        {
            var result = await _svc.CheckOutAsync(id, ct);
            if (result is null)
                return NotFound(ApiResponse<object>.Failure("NOT_FOUND", "记录不存在", HttpContext.TraceIdentifier));

            return Ok(ApiResponse<CheckInOutDto>.Success(result, HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Failure("CHECK_OUT_FAILED", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    // 场馆容量列表
    [HttpGet("venues")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VenueStatusDto>>>> GetVenueStatus(CancellationToken ct)
    {
        var list = await _svc.GetVenueStatusAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<VenueStatusDto>>.Success(list, HttpContext.TraceIdentifier));
    }

    // 场馆内在场人员
    [HttpGet("active/{venueId:int}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CheckInOutDto>>>> GetActive(int venueId, CancellationToken ct)
    {
        var list = await _svc.GetActiveCheckInsAsync(venueId, ct);
        return Ok(ApiResponse<IReadOnlyList<CheckInOutDto>>.Success(list, HttpContext.TraceIdentifier));
    }

    // 入场记录分页
    [HttpGet("records")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CheckInOutDto>>>> GetRecords(
        [FromQuery] int venueId = 0,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var list = await _svc.GetPagedAsync(venueId, pageNumber, pageSize, ct);
        return Ok(ApiResponse<IReadOnlyList<CheckInOutDto>>.Success(list, HttpContext.TraceIdentifier));
    }

    // 容量日志分页
    [HttpGet("capacity-logs")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CapacityLogDto>>>> GetCapacityLogs(
        [FromQuery] int venueId = 0,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var list = await _svc.GetCapacityLogsPagedAsync(venueId, pageNumber, pageSize, ct);
        return Ok(ApiResponse<IReadOnlyList<CapacityLogDto>>.Success(list, HttpContext.TraceIdentifier));
    }

    // 员工首页统计（今日入场、在场人数、场馆实时容量）
    [HttpGet("dashboard-stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats(CancellationToken ct)
    {
        var stats = await _svc.GetDashboardStatsAsync(ct);
        return Ok(ApiResponse<DashboardStatsDto>.Success(stats, HttpContext.TraceIdentifier));
    }

    // 手动触发自动签退（演示/测试用）
    [HttpPost("auto-checkout")]
    public async Task<ActionResult<ApiResponse<object>>> TriggerAutoCheckout(CancellationToken ct)
    {
        try
        {
            var msg = await _svc.TriggerAutoCheckoutAsync(ct);
            return Ok(ApiResponse<object>.Success(new { message = msg }, HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Failure("AUTO_CHECKOUT_FAILED", ex.Message, HttpContext.TraceIdentifier));
        }
    }

    // 会员查询自己的在场记录
    [HttpGet("my-checkin/{cardId:int}")]
    public async Task<ActionResult<ApiResponse<CheckInOutDto>>> GetMyCheckIn(int cardId, CancellationToken ct)
    {
        var record = await _svc.GetMyActiveCheckInAsync(cardId, ct);
        if (record is null)
            return Ok(ApiResponse<CheckInOutDto>.Success(null!, HttpContext.TraceIdentifier));
        return Ok(ApiResponse<CheckInOutDto>.Success(record, HttpContext.TraceIdentifier));
    }

    // 会员查询自己的会员卡信息
    [HttpGet("my-card/{cardId:int}")]
    public async Task<ActionResult<ApiResponse<MemberCardDto>>> GetMyCard(int cardId, CancellationToken ct)
    {
        var card = await _svc.GetMemberCardAsync(cardId, ct);
        if (card is null)
            return NotFound(ApiResponse<object>.Failure("NOT_FOUND", "未找到会员卡", HttpContext.TraceIdentifier));
        return Ok(ApiResponse<MemberCardDto>.Success(card, HttpContext.TraceIdentifier));
    }
}
