// 员工修改卡商品时用的请求 DTO，对应 PUT/PATCH /api/card-products/{id}。

namespace Application.DTOs;

public sealed class UpdateCardProductRequestDto
{
    // 新的商品类型编码，不传就不改
    public string? ProductType { get; init; }

    // 新的标准价格，不传就不改
    public decimal? StandardPrice { get; init; }

    // 是否上架，false 时在编码前加 INACTIVE_ 前缀表示下架
    public bool? IsActive { get; init; }
}
