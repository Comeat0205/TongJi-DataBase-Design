using System;
using System.Collections.Generic;

namespace Domain.Entities;

/// <summary>
/// ½¡ÉíÆ÷²Ä±í
/// </summary>
public partial class Equipment
{
    public int EquipId { get; set; }

    public string EquipName { get; set; } = null!;

    public string? VenueId { get; set; }

    public string? Status { get; set; }

    public DateTime? PurchaseDate { get; set; }

    public virtual ICollection<Repairrecord> Repairrecords { get; set; } = new List<Repairrecord>();
}



