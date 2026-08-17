using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class TimeCardExtension
{
    public int CardId { get; set; }

    public DateTime ExpireDate { get; set; }

    public virtual MemberBenefitCard Card { get; set; } = null!;
}



