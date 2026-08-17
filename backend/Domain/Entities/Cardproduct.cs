using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Cardproduct
{
    public int ProductId { get; set; }

    public string CardType { get; set; } = null!;

    public virtual ICollection<MemberBenefitCard> MemberBenefitCards { get; set; } = new List<MemberBenefitCard>();
}



