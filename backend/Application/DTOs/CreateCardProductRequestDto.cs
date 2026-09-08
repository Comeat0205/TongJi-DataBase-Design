// 员工新增卡商品时的请求 DTO。

namespace Application.DTOs;

public sealed class CreateCardProductRequestDto
{
    // 商品编码，例如 MEMBERSHIP_TIME_90、MEMBERSHIP_COUNT_20
    public string ProductType { get; init; } = string.Empty;

    // 标准定价
    public decimal StandardPrice { get; init; }
}
