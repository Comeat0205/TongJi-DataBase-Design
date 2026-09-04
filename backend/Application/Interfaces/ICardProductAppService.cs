// 卡商品应用服务接口。

using Application.DTOs;

namespace Application.Interfaces;

public interface ICardProductAppService
{
    // 会员购卡页：仅在售商品
    Task<IReadOnlyList<CardProductDto>> GetMembershipProductsAsync(CancellationToken cancellationToken = default);

    // 员工管理页：含下架商品
    Task<IReadOnlyList<CardProductDto>> GetManageListAsync(CancellationToken cancellationToken = default);

    // 新增商品
    Task<CardProductDto> CreateAsync(CreateCardProductRequestDto request, CancellationToken cancellationToken = default);

    // 全量更新
    Task<CardProductDto> UpdateAsync(int priceId, UpdateCardProductRequestDto request, CancellationToken cancellationToken = default);

    // 部分更新（含上架/下架）
    Task<CardProductDto> PatchAsync(int priceId, UpdateCardProductRequestDto request, CancellationToken cancellationToken = default);
}
