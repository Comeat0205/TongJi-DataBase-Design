using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Member
{
    public int MemberId { get; set; }

    public string Name { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? IdCard { get; set; }

    public string? MemberLevel { get; set; }

    public string? Gender { get; set; }

    public DateTime? Birthday { get; set; }

    public DateTime? RegisterDate { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<GroupCourseBooking> GroupCourseBookings { get; set; } = new List<GroupCourseBooking>();

    public virtual ICollection<MemberBenefitCard> MemberBenefitCards { get; set; } = new List<MemberBenefitCard>();

    public virtual ICollection<MemberSchedule> MemberSchedules { get; set; } = new List<MemberSchedule>();
}



