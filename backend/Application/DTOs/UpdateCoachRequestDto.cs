namespace Application.DTOs;

public sealed class UpdateCoachRequestDto
{
    public string DisplayName { get; init; } = string.Empty;
    public string CoachName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Sex { get; init; }
    public string? Specialty { get; init; }
    public string? CoachSummary { get; init; }
}
