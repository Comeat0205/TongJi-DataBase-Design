using System;
using System.Collections.Generic;

namespace Domain.Entities;

/// <summary>
/// 健身器材表
/// </summary>
public partial class Equipment
{
    public int EquipId { get; set; }

    public string EquipName { get; set; } = null!;

    public int? VenueId { get; set; }

    public string? ImageUrl { get; set; }

    public string? Status { get; set; }

    public DateTime? PurchaseDate { get; set; }

    public virtual ICollection<Repairrecord> Repairrecords { get; set; } = new List<Repairrecord>();
}
