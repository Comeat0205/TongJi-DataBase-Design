namespace Application.DTOs;

public sealed class CoachDto
{
    public int CoachId { get; set; }

    public string CoachName { get; set; } = string.Empty;

    public string? Specialty { get; set; }

    public string? Status { get; set; }
}
