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
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var vouchers = await _paymentAppService.GetVouchersAsync(memberId, pageNumber, pageSize, cancellationToken);
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
}
