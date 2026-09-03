using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class CoachRepository : Repository<Coach, int>, ICoachRepository
{
    public CoachRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Coach?> GetByNameAndPhoneAsync(string name, string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await Context.Coaches
            .FirstOrDefaultAsync(x => x.CoachName == name && x.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<Coach?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await Context.Coaches
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task<Coach?> GetByActivePhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await (from coach in Context.Coaches.AsNoTracking()
                      join user in Context.AppUsers.AsNoTracking() on coach.UserId equals user.UserId
                      where coach.PhoneNumber == phoneNumber
                            && user.Status != "0"
                            && (coach.Status == null || coach.Status != "离职")
                      select coach)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> GetNextCoachIdAsync(CancellationToken cancellationToken = default)
    {
        var maxId = await Context.Coaches.MaxAsync(x => (int?)x.CoachId, cancellationToken) ?? 0;
        return maxId + 1;
    }

    public async Task<IReadOnlyList<(Coach Coach, AppUser User)>> GetManagementListAsync(
        string? keyword,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken = default)
    {
        var query = from coach in Context.Coaches.AsNoTracking()
                    join user in Context.AppUsers.AsNoTracking() on coach.UserId equals user.UserId
                    select new { Coach = coach, User = user };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            query = query.Where(x =>
                x.User.LoginName!.Contains(normalizedKeyword) ||
                x.User.DisplayName!.Contains(normalizedKeyword) ||
                x.Coach.CoachName.Contains(normalizedKeyword) ||
                (x.Coach.PhoneNumber != null && x.Coach.PhoneNumber.Contains(normalizedKeyword)) ||
                x.Coach.CoachId.ToString().Contains(normalizedKeyword) ||
                x.User.UserId.ToString().Contains(normalizedKeyword));
        }

        var descending = string.Equals(sortDirection?.Trim(), "desc", StringComparison.OrdinalIgnoreCase);
        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

        query = (normalizedSortBy, descending) switch
        {
            ("userid", false) => query.OrderBy(x => x.User.UserId),
            ("userid", true) => query.OrderByDescending(x => x.User.UserId),
            ("coachid", false) => query.OrderBy(x => x.Coach.CoachId),
            ("coachid", true) => query.OrderByDescending(x => x.Coach.CoachId),
            ("displayname", false) => query.OrderBy(x => x.User.DisplayName).ThenBy(x => x.User.UserId),
            ("displayname", true) => query.OrderByDescending(x => x.User.DisplayName).ThenByDescending(x => x.User.UserId),
            ("hiredate", false) => query.OrderBy(x => x.Coach.HireDate).ThenBy(x => x.User.UserId),
            ("hiredate", true) => query.OrderByDescending(x => x.Coach.HireDate).ThenByDescending(x => x.User.UserId),
            _ => query.OrderBy(x => x.User.UserId)
        };

        var rows = await query.ToListAsync(cancellationToken);
        return rows.Select(x => (x.Coach, x.User)).ToList();
    }
}
