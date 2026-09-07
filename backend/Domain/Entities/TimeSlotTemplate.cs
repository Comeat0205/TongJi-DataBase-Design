using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class TimeSlotTemplate
{
    public string TimeSlotId { get; set; } = null!;

    public virtual ICollection<Groupcourse> Groupcourses { get; set; } = new List<Groupcourse>();

    public virtual ICollection<TimeSlotInstance> TimeSlotInstances { get; set; } = new List<TimeSlotInstance>();
}



