// 会员卡相关 HTTP 接口。

using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/membership-cards")]
public class MembershipCardsController : ControllerBase
{
    private readonly IMembershipCardAppService _membershipCardAppService;
    private readonly IPaymentAppService _paymentAppService;

    public MembershipCardsController(
        IMembershipCardAppService membershipCardAppService,
        IPaymentAppService paymentAppService)
    {
        _membershipCardAppService = membershipCardAppService;
        _paymentAppService = paymentAppService;
    }

    // GET /api/membership-cards?memberId=1  我的卡列表
    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<MembershipCardDto>>),
        StatusCodes.Status200OK)]
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

    // POST /api/membership-cards  直接发卡（内部/测试）
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

    // POST /api/membership-cards/purchase  购卡：生成待支付订单，支付成功后再发卡
    [HttpPost("purchase")]
    [ProducesResponseType(typeof(ApiResponse<PaymentOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PaymentOrderDto>>> Purchase(
        [FromBody] PurchaseMembershipCardRequestDto request,
        CancellationToken cancellationToken)
    {
        var order = await _paymentAppService.CreateMembershipCardOrderAsync(request, cancellationToken);
        return Ok(ApiResponse<PaymentOrderDto>.Success(
            order,
            HttpContext.TraceIdentifier,
            "已创建购卡订单，请前往我的订单支付"));
    }
}
