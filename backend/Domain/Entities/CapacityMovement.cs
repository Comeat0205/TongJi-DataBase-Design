namespace Domain.Entities;

/// <summary>签到/签退进出场流水（记录当时场馆人数与占用率）。</summary>
public class CapacityMovement
{
    public int MovementId { get; set; }

    public int VenueId { get; set; }

    public int MemberId { get; set; }

    public DateTime EventTime { get; set; }

    /// <summary>0=进场，1=出场。</summary>
    public string EventType { get; set; } = "0";

    public int RecordedCount { get; set; }

    public decimal? OccupancyRate { get; set; }

    public int? CheckInOutId { get; set; }

    public virtual Venue? Venue { get; set; }
}
