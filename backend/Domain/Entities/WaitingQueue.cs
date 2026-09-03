using System;

namespace Domain.Entities;

public partial class WaitingQueue
{
    public int QueueId { get; set; }

    public int MemberId { get; set; }

    public int CourseId { get; set; }

    public DateTime? EnqueueTime { get; set; }

    public string QueueStatus { get; set; } = null!;

    public string? Notified { get; set; }

    public virtual Member Member { get; set; } = null!;

    public virtual Groupcourse Course { get; set; } = null!;
}
