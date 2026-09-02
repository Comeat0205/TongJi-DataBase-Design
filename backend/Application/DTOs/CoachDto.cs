namespace Application.DTOs;

public sealed class CoachDto
{
    public int CoachId { get; init; }
    public int UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string CoachName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Sex { get; init; }
    public string? Specialty { get; init; }
    public DateTime? HireDate { get; init; }
    public string? CoachSummary { get; init; }
    public string? Status { get; init; }
}
