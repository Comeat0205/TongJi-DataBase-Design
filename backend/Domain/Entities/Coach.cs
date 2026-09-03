using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Coach
{
    public int CoachId { get; set; }

    public string CoachName { get; set; } = null!;

    public string? Sex { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Specialty { get; set; }

    public DateTime? HireDate { get; set; }

    public string? CoachSummary { get; set; }

    public string? Status { get; set; }

    public int? UserId { get; set; }

    public virtual ICollection<CoachSchedule> CoachSchedules { get; set; } = new List<CoachSchedule>();

    public virtual ICollection<Groupcourse> Groupcourses { get; set; } = new List<Groupcourse>();

    public virtual ICollection<PersonalCourse> PersonalCourses { get; set; } = new List<PersonalCourse>();

    public virtual ICollection<Personalpackage> Personalpackages { get; set; } = new List<Personalpackage>();

    public virtual ICollection<Ptbooking> Ptbookings { get; set; } = new List<Ptbooking>();
}



