namespace Domain.Entities;

public partial class GroupCourseBooking
{
    public int BookingId { get; set; }

    public int MemberId { get; set; }

    public DateTime? BookingTime { get; set; }

    // '0' 默认 / '1' 已预约 / '2' 已取消 / '3' 已完成
    public string? BookingStatus { get; set; }

    public int PackageId { get; set; }

    public virtual GroupPackage Package { get; set; } = null!;

    public virtual Member Member { get; set; } = null!;

    public virtual ICollection<AbsenceRecord> AbsenceRecords { get; set; } = new List<AbsenceRecord>();
}
