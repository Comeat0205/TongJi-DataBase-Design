using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class MemberSchedule
{
    public int ScheduleId { get; set; }

    public int MemberId { get; set; }

    public DateTime ScheduleStart { get; set; }

    public DateTime ScheduleDate { get; set; }

    public DateTime ScheduleEnd { get; set; }

    public string ScheduleType { get; set; } = null!;

    public int? SourceRecordId { get; set; }

    public string? Status { get; set; }

    public virtual Member Member { get; set; } = null!;
}



