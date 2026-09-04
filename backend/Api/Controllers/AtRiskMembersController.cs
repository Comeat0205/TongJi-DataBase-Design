using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/at-risk-members")]
public class AtRiskMembersController : ControllerBase
{
    private readonly IPaymentAppService _paymentAppService;

    public AtRiskMembersController(IPaymentAppService paymentAppService)
    {
        _paymentAppService = paymentAppService;
    }

    /// <summary>
    /// 流失预警会员（功能点 #17）：默认 30 天未入场。
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AtRiskMemberDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AtRiskMemberDto>>>> GetList(
        [FromQuery] int inactiveDays = 30,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var members = await _paymentAppService.GetAtRiskMembersAsync(inactiveDays, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AtRiskMemberDto>>.Success(members, HttpContext.TraceIdentifier));
    }
}
