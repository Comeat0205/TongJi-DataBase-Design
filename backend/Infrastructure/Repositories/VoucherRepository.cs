using Domain.Constants;
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
        string? voucherType,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Vouchers.AsNoTracking().AsQueryable();

        // 已核销不展示；过期作废仅保留 1 天（第 2 天起消失）。
        var keepExpiredFrom = DateTime.Now.Date.AddDays(-1);
        query = query.Where(x =>
            (x.Status == null || x.Status != "1")
            && x.ValidUntil.Date >= keepExpiredFrom
            && (x.VoucherType == VoucherTypes.Birthday
                || x.VoucherType == VoucherTypes.Welcome
                || x.VoucherType == VoucherTypes.StaffDiscount));

        if (memberId is not null)
        {
            query = query.Where(x => x.MemberId == memberId.Value);
        }

        if (!string.IsNullOrWhiteSpace(voucherType))
        {
            query = query.Where(x => x.VoucherType == voucherType);
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
        var member = await Context.Members.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MemberId == memberId, cancellationToken);
        var birthdayStart = member?.Birthday is null ? (DateTime?)null : GetBirthdayInYear(member.Birthday.Value, today.Year);

        var occupiedIds = await Context.PaymentOrders
            .AsNoTracking()
            .Where(o =>
                o.PaymentStatus == "待支付"
                && o.VoucherId != null
                && (excludePendingOrderId == null || o.OrderId != excludePendingOrderId.Value))
            .Select(o => o.VoucherId!.Value)
            .ToListAsync(cancellationToken);

        var vouchers = await Context.Vouchers
            .AsNoTracking()
            .Where(v =>
                v.MemberId == memberId
                && v.Status == "0"
                && v.ValidUntil.Date >= today
                && (v.VoucherType == VoucherTypes.Birthday
                    || v.VoucherType == VoucherTypes.Welcome
                    || v.VoucherType == VoucherTypes.StaffDiscount)
                && !occupiedIds.Contains(v.VoucherId))
            .OrderByDescending(v => v.DiscountValue)
            .ThenBy(v => v.ValidUntil)
            .ThenBy(v => v.VoucherId)
            .ToListAsync(cancellationToken);

        return vouchers
            .Where(v => IsEffectiveFromToday(v, member, birthdayStart, today))
            .ToList();
    }

    public async Task<Voucher?> GetByIdTrackedAsync(int voucherId, CancellationToken cancellationToken = default)
    {
        return await Context.Vouchers.FirstOrDefaultAsync(x => x.VoucherId == voucherId, cancellationToken);
    }

    public async Task<int> GetNextVoucherIdAsync(CancellationToken cancellationToken = default)
    {
        var max = await Context.Vouchers.MaxAsync(x => (int?)x.VoucherId, cancellationToken) ?? 0;
        return max + 1;
    }

    public async Task<bool> HasVoucherAsync(int memberId, string voucherType, CancellationToken cancellationToken = default)
    {
        // 先按会员取出类型再在内存比较，避免 Oracle VARCHAR2 与 NVARCHAR 参数比较报错。
        var types = await Context.Vouchers
            .AsNoTracking()
            .Where(v => v.MemberId == memberId)
            .Select(v => v.VoucherType)
            .ToListAsync(cancellationToken);

        return types.Any(t => string.Equals(t?.Trim(), voucherType.Trim(), StringComparison.Ordinal));
    }

    public async Task<bool> HasBirthdayVoucherForYearAsync(
        int memberId,
        int year,
        CancellationToken cancellationToken = default)
    {
        var member = await Context.Members.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MemberId == memberId, cancellationToken);
        if (member?.Birthday is null)
        {
            return false;
        }

        var birthdayThisYear = GetBirthdayInYear(member.Birthday.Value, year);
        var validUntil = birthdayThisYear.AddMonths(1).Date;

        // 先取回再在内存判断，避免 Oracle 对 AnyAsync 的 True/False 以及 NVARCHAR 比较问题。
        var vouchers = await Context.Vouchers
            .AsNoTracking()
            .Where(v => v.MemberId == memberId)
            .Select(v => new { v.VoucherType, v.ValidUntil })
            .ToListAsync(cancellationToken);

        return vouchers.Any(v =>
            string.Equals(v.VoucherType?.Trim(), VoucherTypes.Birthday, StringComparison.Ordinal)
            && v.ValidUntil.Date == validUntil);
    }

    public async Task<IReadOnlyList<(Member Member, DateTime? LastCheckInTime, int UnusedVoucherCount)>> GetAtRiskMembersAsync(
        int inactiveDays,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // 仅基于 CHECKINOUT 真实签到；最后签到日 ≤ 今天 - N 天
        var cutoffDate = DateTime.Now.Date.AddDays(-inactiveDays);
        var cutoffExclusiveEnd = cutoffDate.AddDays(1);

        var lastCheckInRows = await Context.Checkinouts
            .AsNoTracking()
            .Where(cio => cio.CardId != null)
            .Join(
                Context.MemberBenefitCards.AsNoTracking(),
                cio => cio.CardId!.Value,
                card => card.CardId,
                (cio, card) => new { card.MemberId, cio.CheckInTime })
            .GroupBy(x => x.MemberId)
            .Select(g => new
            {
                MemberId = g.Key,
                LastCheckInTime = g.Max(x => x.CheckInTime)
            })
            .Where(x => x.LastCheckInTime < cutoffExclusiveEnd)
            .ToListAsync(cancellationToken);

        if (lastCheckInRows.Count == 0)
        {
            return Array.Empty<(Member, DateTime?, int)>();
        }

        var lastCheckInByMember = lastCheckInRows.ToDictionary(x => x.MemberId, x => x.LastCheckInTime);
        var memberIds = lastCheckInByMember.Keys.ToList();

        var unusedVoucherRows = await Context.Vouchers
            .AsNoTracking()
            .Where(v =>
                memberIds.Contains(v.MemberId)
                && v.Status == "0"
                && (v.VoucherType == VoucherTypes.Birthday
                    || v.VoucherType == VoucherTypes.Welcome
                    || v.VoucherType == VoucherTypes.StaffDiscount))
            .GroupBy(v => v.MemberId)
            .Select(g => new { MemberId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var unusedByMember = unusedVoucherRows.ToDictionary(x => x.MemberId, x => x.Count);

        var members = await Context.Members
            .AsNoTracking()
            .Where(m => memberIds.Contains(m.MemberId) && (m.Status == null || m.Status != "0"))
            .ToListAsync(cancellationToken);

        var today = DateTime.Now.Date;

        return members
            .Select(m =>
            {
                var last = lastCheckInByMember[m.MemberId];
                unusedByMember.TryGetValue(m.MemberId, out var unused);
                var inactive = Math.Max((today - last.Date).Days, 0);
                return (Member: m, LastCheckInTime: (DateTime?)last, UnusedVoucherCount: unused, InactiveDays: inactive);
            })
            .OrderByDescending(x => x.InactiveDays)
            .ThenBy(x => x.Member.MemberId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => (x.Member, x.LastCheckInTime, x.UnusedVoucherCount))
            .ToList();
    }

    private static bool IsEffectiveFromToday(
        Voucher voucher,
        Member? member,
        DateTime? birthdayStart,
        DateTime today)
    {
        if (voucher.VoucherType == VoucherTypes.Birthday)
        {
            return birthdayStart is not null && today >= birthdayStart.Value.Date;
        }

        if (voucher.VoucherType == VoucherTypes.Welcome)
        {
            return member?.RegisterDate is null || today >= member.RegisterDate.Value.Date;
        }

        return true;
    }

    private static DateTime GetBirthdayInYear(DateTime birthday, int year)
    {
        var day = Math.Min(birthday.Day, DateTime.DaysInMonth(year, birthday.Month));
        return new DateTime(year, birthday.Month, day);
    }
}
