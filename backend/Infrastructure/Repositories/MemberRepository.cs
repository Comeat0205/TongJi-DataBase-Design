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
        // 避免 EF 把 Any 翻译成 Oracle 不支持的 TRUE/FALSE（ORA-00904）。
        var existing = await Context.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdCard == idCard, cancellationToken);
        return existing is not null;
    }

    public async Task<bool> ExistsByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var existing = await Context.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber, cancellationToken);
        return existing is not null;
    }

    public async Task<IReadOnlyList<Member>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        // 查询场景默认使用 AsNoTracking，减少不必要的 EF 变更跟踪开销。
        return await Context.Members
            .AsNoTracking()
            .OrderBy(x => x.MemberId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextMemberIdAsync(CancellationToken cancellationToken = default)
    {
        var maxId = await Context.Members.MaxAsync(x => (int?)x.MemberId, cancellationToken) ?? 0;
        return maxId + 1;
    }
}
