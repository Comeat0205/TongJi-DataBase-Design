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

        return await Context.Vouchers.AsNoTracking()
            .AnyAsync(v =>
                v.MemberId == memberId
                && v.VoucherType == VoucherTypes.Birthday
                && v.ValidUntil.Date == validUntil,
                cancellationToken);
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
                UnusedVoucherCount = Context.Vouchers.Count(v =>
                    v.MemberId == m.MemberId
                    && v.Status == "0"
                    && (v.VoucherType == VoucherTypes.Birthday
                        || v.VoucherType == VoucherTypes.Welcome
                        || v.VoucherType == VoucherTypes.StaffDiscount))
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
