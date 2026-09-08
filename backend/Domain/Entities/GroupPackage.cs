// 团课课包实体，对应表 GROUPPACKAGE。

namespace Domain.Entities;

public partial class GroupPackage
{
    public int PackageId { get; set; }

    public int MemberId { get; set; }

    // DDL 外键指向 GROUPCOURSE；业务上按该课所属课程类型核销。
    public int CourseId { get; set; }

    public int TotalCount { get; set; }

    public int RemainingCount { get; set; }

    // '1'=可用，'0'=已用完/停用，'2'=作废
    public string? PackageStatus { get; set; }

    public virtual Member Member { get; set; } = null!;

    public virtual Groupcourse Course { get; set; } = null!;

    public virtual ICollection<GroupCourseBooking> GroupCourseBookings { get; set; } = new List<GroupCourseBooking>();
}
