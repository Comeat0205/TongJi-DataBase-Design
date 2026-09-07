using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class PersonalCourse
{
    public int PersonalCourseId { get; set; }

    public string CourseName { get; set; } = null!;

    public string? CourseDescription { get; set; }

    public int? CoachId { get; set; }

    public virtual Coach? Coach { get; set; }

    public virtual ICollection<Personalpackage> Personalpackages { get; set; } = new List<Personalpackage>();
}



