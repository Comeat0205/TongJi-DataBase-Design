using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Groupcourse
{
    public int CourseId { get; set; }

    public string CourseName { get; set; } = null!;

    public short MaxCapacity { get; set; }

    public short? CurrentCapacity { get; set; }

    public string? CourseSummary { get; set; }

    public int TypeId { get; set; }

    public int CoachId { get; set; }

    public string TimeSlotId { get; set; } = null!;

    public virtual Coach Coach { get; set; } = null!;

    public virtual ICollection<GroupCourseBooking> GroupCourseBookings { get; set; } = new List<GroupCourseBooking>();

    public virtual ICollection<WaitingQueue> WaitingQueues { get; set; } = new List<WaitingQueue>();

    public virtual TimeSlotTemplate TimeSlot { get; set; } = null!;

    public virtual CourseType Type { get; set; } = null!;
}



