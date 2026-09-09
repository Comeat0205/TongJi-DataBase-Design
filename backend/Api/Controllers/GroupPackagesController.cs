using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/group-packages")]
public class GroupPackagesController : ControllerBase
{
    private readonly IGroupPackageAppService _groupPackageAppService;
    private readonly IPaymentAppService _paymentAppService;

    public GroupPackagesController(
        IGroupPackageAppService groupPackageAppService,
        IPaymentAppService paymentAppService)
    {
        _groupPackageAppService = groupPackageAppService;
        _paymentAppService = paymentAppService;
    }

    [HttpGet("member/{memberId:int}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GroupPackageDto>>>> GetByMember(
        int memberId,
        CancellationToken cancellationToken)
    {
        var data = await _groupPackageAppService.GetByMemberIdAsync(memberId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<GroupPackageDto>>.Success(data, HttpContext.TraceIdentifier));
    }

    [HttpGet("products")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GroupPackageProductDto>>>> GetProducts(
        CancellationToken cancellationToken)
    {
        var data = await _groupPackageAppService.GetProductsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<GroupPackageProductDto>>.Success(data, HttpContext.TraceIdentifier));
    }

    [HttpGet("products/manage")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GroupPackageProductDto>>>> GetManageProducts(
        CancellationToken cancellationToken)
    {
        var data = await _groupPackageAppService.GetManageProductsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<GroupPackageProductDto>>.Success(data, HttpContext.TraceIdentifier));
    }

    [HttpPost("products")]
    public async Task<ActionResult<ApiResponse<GroupPackageProductDto>>> CreateProduct(
        [FromBody] CreateGroupPackageProductRequestDto request,
        CancellationToken cancellationToken)
    {
        var data = await _groupPackageAppService.CreateProductAsync(request, cancellationToken);
        return Ok(ApiResponse<GroupPackageProductDto>.Success(data, HttpContext.TraceIdentifier, "课包商品已创建"));
    }

    [HttpPatch("products/{priceId:int}")]
    public async Task<ActionResult<ApiResponse<GroupPackageProductDto>>> PatchProduct(
        int priceId,
        [FromBody] UpdateGroupPackageProductRequestDto request,
        CancellationToken cancellationToken)
    {
        var data = await _groupPackageAppService.UpdateProductAsync(priceId, request, cancellationToken);
        return Ok(ApiResponse<GroupPackageProductDto>.Success(data, HttpContext.TraceIdentifier, "课包商品已更新"));
    }

    // 购买：创建待支付订单，支付成功后履约开通课包
    [HttpPost("purchase")]
    public async Task<ActionResult<ApiResponse<PaymentOrderDto>>> Purchase(
        [FromBody] PurchaseGroupPackageRequestDto request,
        CancellationToken cancellationToken)
    {
        var order = await _paymentAppService.CreateGroupPackageOrderAsync(request, cancellationToken);
        return Ok(ApiResponse<PaymentOrderDto>.Success(order, HttpContext.TraceIdentifier, "下单成功，请前往我的订单完成支付"));
    }
}
