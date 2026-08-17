using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Capacitylog
{
    public int CapacityLogId { get; set; }

    public int VenueId { get; set; }

    public DateTime? LogTimestamp { get; set; }

    public int? RecordedCapacity { get; set; }

    public int RecordedCount { get; set; }

    public decimal? OccupancyRate { get; set; }

    public virtual Venue Venue { get; set; } = null!;
}



