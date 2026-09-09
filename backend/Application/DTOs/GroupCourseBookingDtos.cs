namespace Application.DTOs;

public sealed class GroupCourseBookingRequestDto
{
    public int MemberId { get; set; }
    public int CourseId { get; set; }
    public int PackageId { get; set; }
    /// <summary>预约的具体上课日（周课中的某一天）。</summary>
    public DateTime? CourseDate { get; set; }
}

public sealed class GroupCourseBookingDto
{
    public int BookingId { get; set; }
    public int MemberId { get; set; }
    public int PackageId { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int TypeId { get; set; }
    public string CourseTypeName { get; set; } = string.Empty;
    public DateTime? BookingTime { get; set; }
    public string BookingStatus { get; set; } = string.Empty;
    public string BookingStatusLabel { get; set; } = string.Empty;
    public DateTime? CourseDate { get; set; }
    public DateTime? CourseStartTime { get; set; }
    public bool CanCancel { get; set; }
    public string Message { get; set; } = string.Empty;
}
