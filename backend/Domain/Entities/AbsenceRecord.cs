using System;

namespace Domain.Entities;

public partial class AbsenceRecord
{
    public int AbsenceId { get; set; }

    public int MemberId { get; set; }

    public int BookingId { get; set; }

    public DateTime CourseDate { get; set; }

    public DateTime? AbsenceTime { get; set; }

    public virtual Member Member { get; set; } = null!;

    public virtual GroupCourseBooking Booking { get; set; } = null!;
}
