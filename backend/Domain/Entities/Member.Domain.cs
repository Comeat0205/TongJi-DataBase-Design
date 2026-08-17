using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

public partial class Member
{
    public MemberStatus GetStatus()
    {
        // 当前数据库中的状态仍然是字符串，这里集中转换成领域枚举。
        return Status switch
        {
            "1" => MemberStatus.Active,
            "2" => MemberStatus.Frozen,
            "3" => MemberStatus.Cancelled,
            _ => MemberStatus.Inactive
        };
    }

    public void SetStatus(MemberStatus status)
    {
        Status = ((int)status).ToString();
    }

    public void Activate()
    {
        // 领域规则放在实体行为里，避免散落在 Controller 或 Repository 中。
        if (GetStatus() == MemberStatus.Cancelled)
        {
            throw new DomainException("已注销会员不能直接恢复为有效状态。");
        }

        SetStatus(MemberStatus.Active);
    }

    public void Freeze()
    {
        if (GetStatus() == MemberStatus.Cancelled)
        {
            throw new DomainException("已注销会员不能冻结。");
        }

        SetStatus(MemberStatus.Frozen);
    }

    public void Cancel()
    {
        SetStatus(MemberStatus.Cancelled);
    }

    public Gender GetGender()
    {
        // 先兼容数据库中的字符值，后续如果表结构调整，只需要修改这一处。
        return Gender?.ToUpperInvariant() switch
        {
            "M" => Enums.Gender.Male,
            "F" => Enums.Gender.Female,
            _ => Enums.Gender.Unknown
        };
    }

    public void SetGender(Gender gender)
    {
        Gender = gender switch
        {
            Enums.Gender.Male => "M",
            Enums.Gender.Female => "F",
            _ => null
        };
    }

    public bool IsActive()
    {
        return GetStatus() == MemberStatus.Active;
    }
}
