using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class MemberBenefitCard
{
    public int CardId { get; set; }

    public int MemberId { get; set; }

    public DateTime? IssueDate { get; set; }

    public string? CardStatus { get; set; }

    public string? CardType { get; set; }

    public virtual ICollection<Checkinout> Checkinouts { get; set; } = new List<Checkinout>();

    public virtual CountCardExtension? CountCardExtension { get; set; }

    public virtual Member Member { get; set; } = null!;

    public virtual TimeCardExtension? TimeCardExtension { get; set; }
}



