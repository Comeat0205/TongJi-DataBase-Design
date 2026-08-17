using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Checkinout
{
    public int CheckInOutId { get; set; }

    public int VenueId { get; set; }

    public int? CardId { get; set; }

    public DateTime CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public string? CheckOutMode { get; set; }

    public virtual MemberBenefitCard? Card { get; set; }

    public virtual Venue Venue { get; set; } = null!;
}



