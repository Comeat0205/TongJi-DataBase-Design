using System;
using System.Collections.Generic;

namespace Domain.Entities;

/// <summary>
/// 员工信息表
/// </summary>
public partial class Employee
{
    public int EmpId { get; set; }

    public string EmpName { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string? Phone { get; set; }

    /// <summary>
    /// 1-在职，0-离职
    /// </summary>
    public string? Status { get; set; }

    public virtual ICollection<Inspectiontask> Inspectiontasks { get; set; } = new List<Inspectiontask>();

    public virtual ICollection<Repairrecord> Repairrecords { get; set; } = new List<Repairrecord>();
}



