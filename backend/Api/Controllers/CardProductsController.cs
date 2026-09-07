// 卡商品 HTTP 接口：会员只读 + 员工维护。

using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/card-products")]
public class CardProductsController : ControllerBase
{
    private readonly ICardProductAppService _cardProductAppService;

    public CardProductsController(ICardProductAppService cardProductAppService)
    {
        _cardProductAppService = cardProductAppService;
    }

    // GET /api/card-products  会员购卡页：在售商品
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CardProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CardProductDto>>>> GetMembershipProducts(
        CancellationToken cancellationToken)
    {
        var products = await _cardProductAppService.GetMembershipProductsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CardProductDto>>.Success(products, HttpContext.TraceIdentifier));
    }

    // GET /api/card-products/manage  员工管理列表
    [HttpGet("manage")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CardProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CardProductDto>>>> GetManageList(
        CancellationToken cancellationToken)
    {
        var products = await _cardProductAppService.GetManageListAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CardProductDto>>.Success(products, HttpContext.TraceIdentifier));
    }

    // POST /api/card-products  新增商品
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CardProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CardProductDto>>> Create(
        [FromBody] CreateCardProductRequestDto request,
        CancellationToken cancellationToken)
    {
        var product = await _cardProductAppService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<CardProductDto>.Success(product, HttpContext.TraceIdentifier, "商品创建成功"));
    }

    // PUT /api/card-products/{priceId}  全量更新
    [HttpPut("{priceId:int}")]
    [ProducesResponseType(typeof(ApiResponse<CardProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CardProductDto>>> Update(
        int priceId,
        [FromBody] UpdateCardProductRequestDto request,
        CancellationToken cancellationToken)
    {
        var product = await _cardProductAppService.UpdateAsync(priceId, request, cancellationToken);
        return Ok(ApiResponse<CardProductDto>.Success(product, HttpContext.TraceIdentifier, "商品更新成功"));
    }

    // PATCH /api/card-products/{priceId}  部分更新（含上架/下架）
    [HttpPatch("{priceId:int}")]
    [ProducesResponseType(typeof(ApiResponse<CardProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CardProductDto>>> Patch(
        int priceId,
        [FromBody] UpdateCardProductRequestDto request,
        CancellationToken cancellationToken)
    {
        var product = await _cardProductAppService.PatchAsync(priceId, request, cancellationToken);
        return Ok(ApiResponse<CardProductDto>.Success(product, HttpContext.TraceIdentifier, "商品更新成功"));
    }
}
