using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class VoucherRepository : Repository<Voucher, int>, IVoucherRepository
{
    public VoucherRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Voucher>> GetListAsync(
        int? memberId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Vouchers.AsNoTracking().AsQueryable();

        // 已核销不展示；过期作废仅保留 1 天（第 2 天起消失）。
        var keepExpiredFrom = DateTime.Now.Date.AddDays(-1);
        query = query.Where(x =>
            (x.Status == null || x.Status != "1")
            && x.ValidUntil.Date >= keepExpiredFrom);

        if (memberId is not null)
        {
            query = query.Where(x => x.MemberId == memberId.Value);
        }

        return await query
            .OrderByDescending(x => x.ValidUntil)
            .ThenByDescending(x => x.VoucherId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Voucher>> GetAvailableAsync(
        int memberId,
        int? excludePendingOrderId,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.Now.Date;

        // 被其他「待支付」订单占用的券不可再选；当前订单已选券可保留。
        var occupiedIds = await Context.PaymentOrders
            .AsNoTracking()
            .Where(o =>
                o.PaymentStatus == "待支付"
                && o.VoucherId != null
                && (excludePendingOrderId == null || o.OrderId != excludePendingOrderId.Value))
            .Select(o => o.VoucherId!.Value)
            .ToListAsync(cancellationToken);

        return await Context.Vouchers
            .AsNoTracking()
            .Where(v =>
                v.MemberId == memberId
                && v.Status == "0"
                && v.ValidUntil.Date >= today
                && !occupiedIds.Contains(v.VoucherId))
            .OrderByDescending(v => v.DiscountValue)
            .ThenBy(v => v.ValidUntil)
            .ThenBy(v => v.VoucherId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Voucher?> GetByIdTrackedAsync(int voucherId, CancellationToken cancellationToken = default)
    {
        return await Context.Vouchers.FirstOrDefaultAsync(x => x.VoucherId == voucherId, cancellationToken);
    }

    public async Task<IReadOnlyList<(Member Member, DateTime? LastCheckInTime, int UnusedVoucherCount)>> GetAtRiskMembersAsync(
        int inactiveDays,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.Now.Date.AddDays(-inactiveDays);

        var members = await Context.Members
            .AsNoTracking()
            .Where(m => m.Status == null || m.Status != "3")
            .Select(m => new
            {
                Member = m,
                LastCheckInTime = Context.MemberBenefitCards
                    .Where(c => c.MemberId == m.MemberId)
                    .SelectMany(c => c.Checkinouts)
                    .Select(cio => (DateTime?)cio.CheckInTime)
                    .Max(),
                UnusedVoucherCount = Context.Vouchers.Count(v => v.MemberId == m.MemberId && v.Status == "0")
            })
            .Where(x => x.LastCheckInTime == null || x.LastCheckInTime < cutoff)
            .OrderBy(x => x.LastCheckInTime ?? DateTime.MinValue)
            .ThenBy(x => x.Member.MemberId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return members
            .Select(x => (x.Member, x.LastCheckInTime, x.UnusedVoucherCount))
            .ToList();
    }
}
