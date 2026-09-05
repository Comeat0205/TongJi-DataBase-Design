namespace Application.DTOs;

public sealed class GroupCourseBookingDto
{
    public int BookingId { get; set; }
    public int MemberId { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public DateTime? BookingTime { get; set; }
    public string BookingStatus { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
