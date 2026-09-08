// 会员卡实体上的业务判断方法，Validity 相关规则放这里。

namespace Domain.Entities;

public partial class MemberBenefitCard
{
    // 判断卡状态字段是不是"有效"，以 DDL 为准：CARD_STATUS='1'
    public bool IsStatusActive()
    {
        return CardStatus == "1";
    }

    // 判断这张卡现在能不能用（状态 + 扩展表一起算）
    public bool IsValidNow()
    {
        if (!IsStatusActive())
        {
            return false;
        }

        // 次卡：还要看剩余次数
        if (CardType == "0")
        {
            if (CountCardExtension == null)
            {
                return false;
            }

            return CountCardExtension.RemainingCount > 0;
        }

        // 时效卡：还要看有没有过期
        if (CardType == "1")
        {
            if (TimeCardExtension == null)
            {
                return false;
            }

            return TimeCardExtension.ExpireDate.Date >= DateTime.Today;
        }

        return false;
    }
}
