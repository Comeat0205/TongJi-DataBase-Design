namespace Application.DTOs;

public sealed class PersonalPackageProductDto
{
    public int PriceId { get; init; }
    public string ProductType { get; init; } = string.Empty;
    public int PersonalCourseId { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public string? CourseDescription { get; init; }
    public int CoachId { get; init; }
    public string CoachName { get; init; } = string.Empty;
    public short TotalSessions { get; init; }
    public int ValidDays { get; init; }
    public decimal Price { get; init; }
    public bool IsActive { get; init; }
}
