using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentOrdersController : ControllerBase
{
    private readonly IPaymentAppService _paymentAppService;

    public PaymentOrdersController(IPaymentAppService paymentAppService)
    {
        _paymentAppService = paymentAppService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PaymentOrderDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PaymentOrderDto>>>> GetList(
        [FromQuery] int? memberId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var orders = await _paymentAppService.GetOrdersAsync(memberId, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PaymentOrderDto>>.Success(orders, HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// 创建待支付订单；未指定券时自动选最优券（优惠最多，同额优先将过期）。
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PaymentOrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaymentOrderDto>>> Create(
        [FromBody] CreatePaymentOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var order = await _paymentAppService.CreateOrderAsync(request, cancellationToken);
        return Ok(ApiResponse<PaymentOrderDto>.Success(order, HttpContext.TraceIdentifier, "下单成功"));
    }

    /// <summary>
    /// 待支付订单更换/取消优惠券（一次仅一张）。
    /// </summary>
    [HttpPut("{orderId:int}/voucher")]
    [ProducesResponseType(typeof(ApiResponse<PaymentOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PaymentOrderDto>>> UpdateVoucher(
        int orderId,
        [FromBody] UpdateOrderVoucherRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var order = await _paymentAppService.UpdateOrderVoucherAsync(orderId, request, cancellationToken);
        if (order is null)
        {
            return NotFound(ApiResponse<object>.Failure("NOT_FOUND", $"未找到编号为 {orderId} 的订单。", HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<PaymentOrderDto>.Success(order, HttpContext.TraceIdentifier, "优惠券已更新"));
    }

    [HttpPost("{orderId:int}/pay")]
    [ProducesResponseType(typeof(ApiResponse<PaymentOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PaymentOrderDto>>> Pay(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _paymentAppService.PayOrderAsync(orderId, cancellationToken);
        if (order is null)
        {
            return NotFound(ApiResponse<object>.Failure("NOT_FOUND", $"未找到编号为 {orderId} 的订单。", HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<PaymentOrderDto>.Success(order, HttpContext.TraceIdentifier, "支付成功"));
    }

    /// <summary>
    /// 取消订单。待支付可取消；已支付取消退实付但不退券。
    /// </summary>
    [HttpPost("{orderId:int}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<PaymentOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PaymentOrderDto>>> Cancel(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _paymentAppService.CancelOrderAsync(orderId, cancellationToken);
        if (order is null)
        {
            return NotFound(ApiResponse<object>.Failure("NOT_FOUND", $"未找到编号为 {orderId} 的订单。", HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<PaymentOrderDto>.Success(order, HttpContext.TraceIdentifier, order.ActionMessage ?? "订单已取消"));
    }
}
