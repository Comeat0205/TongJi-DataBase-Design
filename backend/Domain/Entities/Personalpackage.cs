using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Personalpackage
{
    public int PackageId { get; set; }

    public int MemberId { get; set; }

    public int CoachId { get; set; }

    public short TotalSessions { get; set; }

    public short RemainingSessions { get; set; }

    public DateTime ExpireDate { get; set; }

    public string PackageStatus { get; set; } = null!;

    public int PersonalCourseId { get; set; }

    public virtual Coach Coach { get; set; } = null!;

    public virtual PersonalCourse PersonalCourse { get; set; } = null!;

    public virtual ICollection<Ptbooking> Ptbookings { get; set; } = new List<Ptbooking>();
}



