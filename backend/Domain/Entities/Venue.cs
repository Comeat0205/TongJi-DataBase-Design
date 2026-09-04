using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Venue
{
    public int VenueId { get; set; }

    public string VenueName { get; set; } = null!;

    public short MaxCapacity { get; set; }

    public short? CurrentCapacity { get; set; }

    public string? ImageUrl { get; set; }

    public string? VenueStatus { get; set; }

    public virtual ICollection<Capacitylog> Capacitylogs { get; set; } = new List<Capacitylog>();

    public virtual ICollection<Checkinout> Checkinouts { get; set; } = new List<Checkinout>();
}



