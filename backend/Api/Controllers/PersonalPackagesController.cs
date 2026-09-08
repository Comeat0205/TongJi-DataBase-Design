using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api")]
public sealed class PersonalPackagesController : ControllerBase
{
    private readonly IPersonalPackageAppService _personalPackageAppService;
    private readonly IPaymentAppService _paymentAppService;

    public PersonalPackagesController(
        IPersonalPackageAppService personalPackageAppService,
        IPaymentAppService paymentAppService)
    {
        _personalPackageAppService = personalPackageAppService;
        _paymentAppService = paymentAppService;
    }

    [HttpGet("members/{memberId:int}/personal-packages")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PersonalPackageDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PersonalPackageDto>>>> GetByMemberId(
        int memberId,
        CancellationToken cancellationToken)
    {
        var packages = await _personalPackageAppService.GetByMemberIdAsync(memberId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PersonalPackageDto>>.Success(
            packages,
            HttpContext.TraceIdentifier));
    }

    [HttpGet("personal-package-products")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PersonalPackageProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PersonalPackageProductDto>>>> GetProducts(
        CancellationToken cancellationToken)
    {
        var products = await _personalPackageAppService.GetProductsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PersonalPackageProductDto>>.Success(
            products,
            HttpContext.TraceIdentifier));
    }

    [HttpPost("personal-packages/purchase")]
    [ProducesResponseType(typeof(ApiResponse<PaymentOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PaymentOrderDto>>> Purchase(
        [FromBody] PurchasePersonalPackageRequestDto request,
        CancellationToken cancellationToken)
    {
        var order = await _paymentAppService.CreatePersonalPackageOrderAsync(request, cancellationToken);
        return Ok(ApiResponse<PaymentOrderDto>.Success(
            order,
            HttpContext.TraceIdentifier,
            "已创建课包订单，请前往我的订单支付"));
    }
}
