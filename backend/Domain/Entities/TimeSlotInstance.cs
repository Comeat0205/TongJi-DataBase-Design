using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class TimeSlotInstance
{
    public string TimeSlotId { get; set; } = null!;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public DateTime CourseDate { get; set; }

    public virtual TimeSlotTemplate TimeSlot { get; set; } = null!;
}



