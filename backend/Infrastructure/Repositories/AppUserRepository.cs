using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class AppUserRepository : Repository<AppUser, int>, IAppUserRepository
{
    public AppUserRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<AppUser?> GetByLoginNameAsync(string loginName, CancellationToken cancellationToken = default)
    {
        return await Context.AppUsers
            .FirstOrDefaultAsync(x => x.LoginName == loginName, cancellationToken);
    }

    public async Task<AppUser?> GetActiveByLoginNameAsync(string loginName, CancellationToken cancellationToken = default)
    {
        return await Context.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.LoginName == loginName && x.Status != "0", cancellationToken);
    }

    public async Task<bool> ExistsByLoginNameAsync(string loginName, CancellationToken cancellationToken = default)
    {
        var existing = await Context.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.LoginName == loginName && x.Status != "0", cancellationToken);
        return existing is not null;
    }

    public async Task<int> GetNextUserIdAsync(CancellationToken cancellationToken = default)
    {
        // 云库当前无序列时用 MAX+1；有序列后可改为 NEXTVAL。
        var maxId = await Context.AppUsers.MaxAsync(x => (int?)x.UserId, cancellationToken) ?? 0;
        return maxId + 1;
    }

    public async Task<bool> ExistsByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await Context.AppUsers
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId, cancellationToken);
    }
}
