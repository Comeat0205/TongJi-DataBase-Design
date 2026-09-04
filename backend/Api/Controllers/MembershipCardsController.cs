// 会员卡相关 HTTP 接口，阶段 2 先做两个只读的 GET。

using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]      // Attribute
[Route("api/membership-cards")]      // 定义这个 Controller 的基础 URL 路径
public class MembershipCardsController : ControllerBase
{
    // 私有只读字段，用来存储会员卡应用服务实例
    private readonly IMembershipCardAppService _membershipCardAppService;

    // 构造函数，接收会员卡应用服务实例作为参数，并赋值给私有字段
    public MembershipCardsController(IMembershipCardAppService membershipCardAppService)
    {
        _membershipCardAppService = membershipCardAppService;
    }

    // GET /api/membership-cards?memberId=1  我的卡列表
    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<MembershipCardDto>>),
        StatusCodes.Status200OK)]
    // 异步执行一个 API 请求，最终返回 HTTP 响应，响应里面包含会员卡 DTO 列表
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MembershipCardDto>>>> GetByMemberId(
        [FromQuery] int memberId,
        CancellationToken cancellationToken)
    {
        var cards = await _membershipCardAppService.GetByMemberIdAsync(memberId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<MembershipCardDto>>.Success(cards, HttpContext.TraceIdentifier));
    }

    // GET /api/membership-cards/101  单张卡详情
    [HttpGet("{cardId:int}")]
    [ProducesResponseType(typeof(ApiResponse<MembershipCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MembershipCardDto>>> GetById(
        int cardId,
        CancellationToken cancellationToken)
    {
        var card = await _membershipCardAppService.GetByIdAsync(cardId, cancellationToken);
        if (card == null)
        {
            return NotFound(ApiResponse<object>.Failure(
                "NOT_FOUND",
                $"未找到编号为 {cardId} 的会员卡。",
                HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<MembershipCardDto>.Success(card, HttpContext.TraceIdentifier));
    }

    // POST /api/membership-cards  直接发卡
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MembershipCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<MembershipCardDto>>> Create(
        [FromBody] CreateMembershipCardRequestDto request,
        CancellationToken cancellationToken)
    {
        var card = await _membershipCardAppService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<MembershipCardDto>.Success(card, HttpContext.TraceIdentifier, "发卡成功"));
    }

    // POST /api/membership-cards/purchase-mock  MVP 模拟支付购卡
    [HttpPost("purchase-mock")]
    [ProducesResponseType(typeof(ApiResponse<MembershipCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<MembershipCardDto>>> PurchaseMock(
        [FromBody] PurchaseMembershipCardRequestDto request,
        CancellationToken cancellationToken)
    {
        var card = await _membershipCardAppService.PurchaseMockAsync(request, cancellationToken);
        return Ok(ApiResponse<MembershipCardDto>.Success(card, HttpContext.TraceIdentifier, "模拟购卡成功"));
    }
}
