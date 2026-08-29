// 会员卡应用服务接口，Controller 只调这一层。

using Application.DTOs;

namespace Application.Interfaces;

public interface IMembershipCardAppService
{
    // 我的卡列表
    Task<IReadOnlyList<MembershipCardDto>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default);

    // 单张卡详情
    Task<MembershipCardDto?> GetByIdAsync(int cardId, CancellationToken cancellationToken = default);
}
