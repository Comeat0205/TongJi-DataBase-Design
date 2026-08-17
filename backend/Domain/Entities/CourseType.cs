using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class CourseType
{
    public int TypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<Groupcourse> Groupcourses { get; set; } = new List<Groupcourse>();
}



