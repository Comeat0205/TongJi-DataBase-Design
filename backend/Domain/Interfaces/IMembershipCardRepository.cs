// 会员卡仓储接口，定义查库要用到的方法。

using Domain.Entities;

namespace Domain.Interfaces;

public interface IMembershipCardRepository : IRepository<MemberBenefitCard, int>
{
    // 查某个会员名下的所有卡，列表页用
    // 异步函数，返回一个只读列表，列表元素为 MemberBenefitCard 类型
    Task<IReadOnlyList<MemberBenefitCard>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default);

    // 查某一张卡的详情，会把扩展表一起查出来
    Task<MemberBenefitCard?> GetDetailByIdAsync(int cardId, CancellationToken cancellationToken = default);
}
