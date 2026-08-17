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

    public async Task<bool> ExistsByIdCardAsync(string idCard, CancellationToken cancellationToken = default)
    {
        return await Context.Members
            .AnyAsync(x => x.IdCard == idCard, cancellationToken);
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
}


