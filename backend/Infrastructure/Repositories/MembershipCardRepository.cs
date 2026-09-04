// 会员卡仓储实现，用 EF Core 查 MEMBER_BENEFIT_CARD 及扩展表。

using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class MembershipCardRepository : Repository<MemberBenefitCard, int>, IMembershipCardRepository
{
    // 调用父类 Repository 的构造函数，并把 context 传给它
    public MembershipCardRepository(AppDbContext context) : base(context)
    {
    }

    // 查某个会员的所有卡，按发卡日期从新到旧排
    public async Task<IReadOnlyList<MemberBenefitCard>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default)
    {
        return await Context.MemberBenefitCards
            .AsNoTracking()
            .Include(x => x.CountCardExtension)
            .Include(x => x.TimeCardExtension)
            .Where(x => x.MemberId == memberId)
            .OrderByDescending(x => x.IssueDate)
            .ThenByDescending(x => x.CardId)
            .ToListAsync(cancellationToken);
    }

    // 按卡编号查详情，扩展表用 Include 相当于 LEFT JOIN
    public async Task<MemberBenefitCard?> GetDetailByIdAsync(int cardId, CancellationToken cancellationToken = default)
    {
        return await Context.MemberBenefitCards
            .AsNoTracking()
            .Include(x => x.CountCardExtension)
            .Include(x => x.TimeCardExtension)
            .FirstOrDefaultAsync(x => x.CardId == cardId, cancellationToken);
    }

    // 用 Oracle 序列表取新卡号（不能用 EF SqlQueryRaw，否则会 ORA-02287）
    public async Task<int> GetNextCardIdAsync(CancellationToken cancellationToken = default)
    {
        await Context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = Context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT GYM_ADMIN.SEQ_MEMBER_BENEFIT_CARD.NEXTVAL FROM DUAL";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }
        finally
        {
            await Context.Database.CloseConnectionAsync();
        }
    }

    // 写入会员卡主表
    public async Task AddCardAsync(MemberBenefitCard card, CancellationToken cancellationToken = default)
    {
        await Context.MemberBenefitCards.AddAsync(card, cancellationToken);
    }

    // 写入次卡扩展表
    public async Task AddCountExtensionAsync(CountCardExtension extension, CancellationToken cancellationToken = default)
    {
        await Context.CountCardExtensions.AddAsync(extension, cancellationToken);
    }

    // 写入时效卡扩展表
    public async Task AddTimeExtensionAsync(TimeCardExtension extension, CancellationToken cancellationToken = default)
    {
        await Context.TimeCardExtensions.AddAsync(extension, cancellationToken);
    }
}
