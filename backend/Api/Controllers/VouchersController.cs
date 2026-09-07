using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VouchersController : ControllerBase
{
    private readonly IPaymentAppService _paymentAppService;

    public VouchersController(IPaymentAppService paymentAppService)
    {
        _paymentAppService = paymentAppService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VoucherDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VoucherDto>>>> GetList(
        [FromQuery] int? memberId,
        [FromQuery] string? voucherType,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var vouchers = await _paymentAppService.GetVouchersAsync(
            memberId,
            voucherType,
            pageNumber,
            pageSize,
            cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<VoucherDto>>.Success(vouchers, HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// 可用券列表（未使用、未过期、未被其他待支付订单占用）。
    /// </summary>
    [HttpGet("available")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VoucherDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VoucherDto>>>> GetAvailable(
        [FromQuery] int memberId,
        [FromQuery] int? forOrderId,
        CancellationToken cancellationToken = default)
    {
        var vouchers = await _paymentAppService.GetAvailableVouchersAsync(memberId, forOrderId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<VoucherDto>>.Success(vouchers, HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// 员工发放折扣券（33 元，有效期 7 天）。
    /// </summary>
    [HttpPost("issue-discount")]
    [ProducesResponseType(typeof(ApiResponse<VoucherDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> IssueDiscount(
        [FromBody] IssueDiscountVoucherRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var voucher = await _paymentAppService.IssueDiscountVoucherAsync(request, cancellationToken);
        return Ok(ApiResponse<VoucherDto>.Success(voucher, HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// 向所有在籍会员各发放一张折扣券（33 元，7 天）。
    /// </summary>
    [HttpPost("issue-discount-all")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<int>>> IssueDiscountAll(CancellationToken cancellationToken = default)
    {
        var count = await _paymentAppService.IssueDiscountVouchersToAllAsync(cancellationToken);
        return Ok(ApiResponse<int>.Success(count, HttpContext.TraceIdentifier, $"已向 {count} 名会员发放折扣券。"));
    }

    /// <summary>
    /// 注册时发放新客体验券（50 元，注册日起 1 年）。每人仅一张。
    /// </summary>
    [HttpPost("issue-welcome/{memberId:int}")]
    [ProducesResponseType(typeof(ApiResponse<VoucherDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> IssueWelcome(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        var voucher = await _paymentAppService.IssueWelcomeVoucherAsync(memberId, cancellationToken);
        return Ok(ApiResponse<VoucherDto>.Success(voucher, HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// 为今日生日的会员发放生日福利券（66 元，生日起 1 个月）。可每日定时调用。
    /// </summary>
    [HttpPost("issue-birthday-today")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<int>>> IssueBirthdayToday(CancellationToken cancellationToken = default)
    {
        var count = await _paymentAppService.IssueBirthdayVouchersForTodayAsync(cancellationToken);
        return Ok(ApiResponse<int>.Success(count, HttpContext.TraceIdentifier));
    }
}
