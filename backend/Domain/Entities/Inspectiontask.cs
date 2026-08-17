using System;
using System.Collections.Generic;

namespace Domain.Entities;

/// <summary>
/// 卫生巡检任务表
/// </summary>
public partial class Inspectiontask
{
    public int TaskId { get; set; }

    public int VenueId { get; set; }

    public int EmpId { get; set; }

    public DateTime TaskTime { get; set; }

    public string? Status { get; set; }

    public string? Remark { get; set; }

    public virtual Employee Emp { get; set; } = null!;
}



