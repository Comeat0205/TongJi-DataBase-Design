using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class CoachSchedule
{
    public int ScheduleId { get; set; }

    public int CoachId { get; set; }

    public DateTime ScheduleStart { get; set; }

    public DateTime ScheduleEnd { get; set; }

    public DateTime ScheduleDate { get; set; }

    public string? ScheduleType { get; set; }

    public int? SourceRecordId { get; set; }

    public string? Status { get; set; }

    public virtual Coach Coach { get; set; } = null!;
}



