// 会员卡应用服务接口，Controller 只调这一层。

using Application.DTOs;

namespace Application.Interfaces;

public interface IMembershipCardAppService
{
    // 我的卡列表
    Task<IReadOnlyList<MembershipCardDto>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default);

    // 单张卡详情
    Task<MembershipCardDto?> GetByIdAsync(int cardId, CancellationToken cancellationToken = default);

    // 直接发卡（内部/测试用；支付成功履约也会调用）
    Task<MembershipCardDto> CreateAsync(CreateMembershipCardRequestDto request, CancellationToken cancellationToken = default);
}
