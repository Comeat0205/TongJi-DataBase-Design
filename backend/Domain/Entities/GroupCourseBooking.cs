using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class GroupCourseBooking
{
    public int BookingId { get; set; }

    public int MemberId { get; set; }

    public int CourseId { get; set; }

    public DateTime? BookingTime { get; set; }

    public string? BookingStatus { get; set; }

    public virtual Groupcourse Course { get; set; } = null!;

    public virtual Member Member { get; set; } = null!;

    public virtual ICollection<AbsenceRecord> AbsenceRecords { get; set; } = new List<AbsenceRecord>();
}



