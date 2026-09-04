using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PaymentOrderRepository : Repository<PaymentOrder, int>, IPaymentOrderRepository
{
    public PaymentOrderRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<PaymentOrder>> GetListAsync(
        int? memberId,
        int? businessOrderId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = Context.PaymentOrders
            .AsNoTracking()
            .Include(x => x.Voucher)
            .Include(x => x.PaymentDetails)
            .AsQueryable();

        // 订单表无 MEMBER_ID：按关联优惠券归属会员筛选；无券订单在会员视角不展示。
        if (memberId is not null)
        {
            query = query.Where(x => x.Voucher != null && x.Voucher.MemberId == memberId.Value);
        }

        if (businessOrderId is not null)
        {
            query = query.Where(x => x.BusinessOrderId == businessOrderId.Value);
        }

        return await query
            .OrderByDescending(x => x.OrderId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentOrder?> GetByIdWithDetailsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await Context.PaymentOrders
            .Include(x => x.Voucher)
            .Include(x => x.PaymentDetails)
            .FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
    }

    public async Task<int> GetNextOrderIdAsync(CancellationToken cancellationToken = default)
    {
        var max = await Context.PaymentOrders.MaxAsync(x => (int?)x.OrderId, cancellationToken) ?? 0;
        return max + 1;
    }

    public async Task<int> GetNextBusinessOrderIdAsync(CancellationToken cancellationToken = default)
    {
        var max = await Context.PaymentOrders.MaxAsync(x => (int?)x.BusinessOrderId, cancellationToken) ?? 90000;
        return max + 1;
    }

    public async Task<int> GetNextDetailIdAsync(CancellationToken cancellationToken = default)
    {
        var max = await Context.PaymentDetails.MaxAsync(x => (int?)x.DetailId, cancellationToken) ?? 0;
        return max + 1;
    }

    public async Task<int?> GetDefaultPriceIdAsync(CancellationToken cancellationToken = default)
    {
        return await Context.PriceLists
            .AsNoTracking()
            .OrderBy(x => x.PriceId)
            .Select(x => (int?)x.PriceId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
