// 会员会籍卡主实体，对应表 MEMBER_BENEFIT_CARD（办卡/我的卡核心数据）。
using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class MemberBenefitCard
{
    public int CardId { get; set; }

    public int MemberId { get; set; }

    public DateTime? IssueDate { get; set; }

    // 卡状态：'0'/'1'/'2'（库 CHECK；默认 '1'）。是否有效以 fn_is_card_valid 及组内约定为准。
    public string? CardStatus { get; set; }

    // 卡类型：'0'/'1'（库 CHECK）。一般对应次卡或时效卡扩展表。
    public string CardType { get; set; } = null!;

    public virtual ICollection<Checkinout> Checkinouts { get; set; } = new List<Checkinout>();

    public virtual CountCardExtension? CountCardExtension { get; set; }

    public virtual Member Member { get; set; } = null!;

    public virtual TimeCardExtension? TimeCardExtension { get; set; }
}
