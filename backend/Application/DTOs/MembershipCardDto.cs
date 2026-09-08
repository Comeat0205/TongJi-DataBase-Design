// 会员卡接口返回用的 DTO，给"我的会员卡"列表和详情页用。

namespace Application.DTOs;

public sealed class MembershipCardDto
{
    // 卡编号，对应 MEMBER_BENEFIT_CARD.CARD_ID
    public int CardId { get; init; }

    // 会员编号，对应 MEMBER_BENEFIT_CARD.MEMBER_ID
    public int MemberId { get; init; }

    // 卡结构类型：'0'=次卡，'1'=时效卡
    public string CardType { get; init; } = string.Empty;

    // 给前端直接显示的中文，例如"次卡""时效卡"
    public string CardTypeLabel { get; init; } = string.Empty;

    // 卡状态：'1'=有效，'0'和'2'=无效（以 DDL 为准）
    public string? CardStatus { get; init; }

    // 发卡日期
    public DateTime? IssueDate { get; init; }

    // 次卡总次数，只有次卡才有值
    public int? TotalCounts { get; init; }

    // 次卡剩余次数，只有次卡才有值
    public int? RemainingCount { get; init; }

    // 时效卡到期日，只有时效卡才有值
    public DateTime? ExpireDate { get; init; }

    // 当前这张卡能不能用，后端会根据状态和扩展表一起算
    public bool IsValid { get; init; }
}
