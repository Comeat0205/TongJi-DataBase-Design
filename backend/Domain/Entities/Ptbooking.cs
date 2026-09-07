using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Ptbooking
{
    public int PtBookingId { get; set; }

    public int PackageId { get; set; }

    public int MemberId { get; set; }

    public int CoachId { get; set; }

    public DateTime BookingTime { get; set; }

    public DateTime SessionTime { get; set; }

    public string CoachConfirmed { get; set; } = null!;

    public string MemberConfirmed { get; set; } = null!;

    public string ConsumeStatus { get; set; } = null!;

    public DateTime? ConsumedTime { get; set; }

    public virtual Coach Coach { get; set; } = null!;

    public virtual Personalpackage Package { get; set; } = null!;
}



