namespace Application.DTOs;

public sealed class PersonalPackageDto
{
    public int PackageId { get; init; }
    public int MemberId { get; init; }
    public int CoachId { get; init; }
    public string CoachName { get; init; } = string.Empty;
    public int PersonalCourseId { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public string? CourseDescription { get; init; }
    public short TotalSessions { get; init; }
    public short RemainingSessions { get; init; }
    public DateTime ExpireDate { get; init; }
    public string PackageStatus { get; init; } = string.Empty;
    public bool IsUsable { get; init; }
}
