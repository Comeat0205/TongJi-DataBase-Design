// 价格表仓储接口，卡商品列表从 PRICE_LIST 读取。

using Domain.Entities;

namespace Domain.Interfaces;

public interface IPriceListRepository : IRepository<PriceList, int>
{
    // 查所有会员卡类商品（PRODUCT_TYPE 以 MEMBERSHIP_ 开头）
    Task<IReadOnlyList<PriceList>> GetMembershipProductsAsync(CancellationToken cancellationToken = default);

    // 员工管理用：含已下架（INACTIVE_ 前缀）的会员卡商品
    Task<IReadOnlyList<PriceList>> GetManageMembershipProductsAsync(CancellationToken cancellationToken = default);

    // 取下一个 PRICE_ID
    Task<int> GetNextPriceIdAsync(CancellationToken cancellationToken = default);
}
