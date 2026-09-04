using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class MemberRepository : Repository<Member, int>, IMemberRepository
{
    public MemberRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Member?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await Context.Members
            .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<Member?> GetByNameAndPhoneAsync(string name, string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await Context.Members
            .FirstOrDefaultAsync(x => x.Name == name && x.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<Member?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await Context.Members
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task<bool> ExistsByIdCardAsync(string idCard, CancellationToken cancellationToken = default)
    {
        var existing = await Context.Members
            .AsNoTracking()
            .Join(Context.AppUsers.AsNoTracking(), member => member.UserId, user => user.UserId, (member, user) => new { Member = member, User = user })
            .FirstOrDefaultAsync(x => x.Member.IdCard == idCard && x.User.Status != "0", cancellationToken);
        return existing is not null;
    }

    public async Task<bool> ExistsByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var existing = await Context.Members
            .AsNoTracking()
            .Join(Context.AppUsers.AsNoTracking(), member => member.UserId, user => user.UserId, (member, user) => new { Member = member, User = user })
            .FirstOrDefaultAsync(x => x.Member.PhoneNumber == phoneNumber && x.User.Status != "0", cancellationToken);
        return existing is not null;
    }

    public async Task<IReadOnlyList<Member>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        return await Context.Members
            .AsNoTracking()
            .OrderBy(x => x.MemberId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(Member Member, AppUser User)>> GetManagementListAsync(
        string? keyword,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken = default)
    {
        var query = from member in Context.Members.AsNoTracking()
                    join user in Context.AppUsers.AsNoTracking() on member.UserId equals user.UserId
                    select new { Member = member, User = user };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            query = query.Where(x =>
                x.User.LoginName!.Contains(normalizedKeyword) ||
                x.User.DisplayName!.Contains(normalizedKeyword) ||
                x.Member.Name.Contains(normalizedKeyword) ||
                (x.Member.PhoneNumber != null && x.Member.PhoneNumber.Contains(normalizedKeyword)) ||
                x.Member.MemberId.ToString().Contains(normalizedKeyword) ||
                x.User.UserId.ToString().Contains(normalizedKeyword));
        }

        var descending = string.Equals(sortDirection?.Trim(), "desc", StringComparison.OrdinalIgnoreCase);
        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

        query = (normalizedSortBy, descending) switch
        {
            ("userid", false) => query.OrderBy(x => x.User.UserId),
            ("userid", true) => query.OrderByDescending(x => x.User.UserId),
            ("memberid", false) => query.OrderBy(x => x.Member.MemberId),
            ("memberid", true) => query.OrderByDescending(x => x.Member.MemberId),
            ("displayname", false) => query.OrderBy(x => x.User.DisplayName).ThenBy(x => x.User.UserId),
            ("displayname", true) => query.OrderByDescending(x => x.User.DisplayName).ThenByDescending(x => x.User.UserId),
            ("registerdate", false) => query.OrderBy(x => x.Member.RegisterDate).ThenBy(x => x.User.UserId),
            ("registerdate", true) => query.OrderByDescending(x => x.Member.RegisterDate).ThenByDescending(x => x.User.UserId),
            _ => query.OrderBy(x => x.User.UserId)
        };

        var rows = await query.ToListAsync(cancellationToken);
        return rows.Select(x => (x.Member, x.User)).ToList();
    }

    public async Task<bool> HasBlockingRelationsAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var hasBooking = await Context.GroupCourseBookings.AsNoTracking().AnyAsync(x => x.MemberId == memberId, cancellationToken);
        if (hasBooking)
        {
            return true;
        }

        var hasBenefitCard = await Context.MemberBenefitCards.AsNoTracking().AnyAsync(x => x.MemberId == memberId, cancellationToken);
        if (hasBenefitCard)
        {
            return true;
        }

        var hasSchedule = await Context.MemberSchedules.AsNoTracking().AnyAsync(x => x.MemberId == memberId, cancellationToken);
        return hasSchedule;
    }

    public async Task<int> GetNextMemberIdAsync(CancellationToken cancellationToken = default)
    {
        var maxId = await Context.Members.MaxAsync(x => (int?)x.MemberId, cancellationToken) ?? 0;
        return maxId + 1;
    }
    public async Task<IReadOnlyList<Member>> GetActiveMembersAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Members
            .AsNoTracking()
            .Where(m => m.Status == null || m.Status != "3")
            .OrderBy(m => m.MemberId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Member>> GetMembersWithBirthdayTodayAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Now.Date;
        var members = await Context.Members
            .AsNoTracking()
            .Where(m => m.Birthday != null && (m.Status == null || m.Status != "3"))
            .ToListAsync(cancellationToken);

        return members
            .Where(m => m.Birthday!.Value.Month == today.Month && m.Birthday.Value.Day == today.Day)
            .OrderBy(m => m.MemberId)
            .ToList();
    }
}
