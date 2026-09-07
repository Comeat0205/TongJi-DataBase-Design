using System;
using System.Collections.Generic;

namespace Domain.Entities;

/// <summary>
/// Æ÷²ÄÎ¬ÐÞ¼ÇÂ¼±í
/// </summary>
public partial class Repairrecord
{
    public int RecordId { get; set; }

    public int EquipId { get; set; }

    public int? EmpId { get; set; }

    public DateTime? ReportTime { get; set; }

    public decimal? RepairCost { get; set; }

    public string? Status { get; set; }

    public string? Description { get; set; }

    public virtual Employee? Emp { get; set; }

    public virtual Equipment Equip { get; set; } = null!;
}



