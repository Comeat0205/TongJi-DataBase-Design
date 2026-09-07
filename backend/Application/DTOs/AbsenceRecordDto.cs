namespace Application.DTOs;

public sealed class AbsenceRecordDto
{
    public int AbsenceId { get; set; }

    public int MemberId { get; set; }

    public int BookingId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public DateTime CourseDate { get; set; }

    public DateTime? AbsenceTime { get; set; }
}
