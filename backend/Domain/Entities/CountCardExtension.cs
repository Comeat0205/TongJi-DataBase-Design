using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class CountCardExtension
{
    public int CardId { get; set; }

    public int TotalCounts { get; set; }

    public int RemainingCount { get; set; }

    public virtual MemberBenefitCard Card { get; set; } = null!;
}



