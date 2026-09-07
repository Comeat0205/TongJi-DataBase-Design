// 价格表仓储实现。

using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PriceListRepository : Repository<PriceList, int>, IPriceListRepository
{
    public PriceListRepository(AppDbContext context) : base(context)
    {
    }

    // 会员购卡页：只取在售商品
    public async Task<IReadOnlyList<PriceList>> GetMembershipProductsAsync(CancellationToken cancellationToken = default)
    {
        return await Context.PriceLists
            .AsNoTracking()
            .Where(x => x.ProductType.StartsWith("MEMBERSHIP_")
                && !x.ProductType.StartsWith("INACTIVE_"))
            .OrderBy(x => x.PriceId)
            .ToListAsync(cancellationToken);
    }

    // 员工管理页：在售 + 已下架
    public async Task<IReadOnlyList<PriceList>> GetManageMembershipProductsAsync(CancellationToken cancellationToken = default)
    {
        return await Context.PriceLists
            .AsNoTracking()
            .Where(x => x.ProductType.Contains("MEMBERSHIP_"))
            .OrderBy(x => x.PriceId)
            .ToListAsync(cancellationToken);
    }

    // 当前最大 PRICE_ID + 1
    public async Task<int> GetNextPriceIdAsync(CancellationToken cancellationToken = default)
    {
        var maxId = await Context.PriceLists.MaxAsync(x => (int?)x.PriceId, cancellationToken);
        return (maxId ?? 0) + 1;
    }
}
