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

    public async Task<bool> ExistsByLoginNameAsync(string loginName, CancellationToken cancellationToken = default)
    {
        // 避免 EF 把 Any 翻译成 Oracle 不支持的 TRUE/FALSE（ORA-00904）。
        var existing = await Context.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.LoginName == loginName, cancellationToken);
        return existing is not null;
    }

    public async Task<int> GetNextUserIdAsync(CancellationToken cancellationToken = default)
    {
        // 云库当前无序列时用 MAX+1；有序列后可改为 NEXTVAL。
        var maxId = await Context.AppUsers.MaxAsync(x => (int?)x.UserId, cancellationToken) ?? 0;
        return maxId + 1;
    }
}
