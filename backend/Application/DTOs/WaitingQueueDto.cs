namespace Application.DTOs;

public sealed class WaitingQueueDto
{
    public int QueueId { get; set; }
    public int MemberId { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public DateTime? EnqueueTime { get; set; }
    public string QueueStatus { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
