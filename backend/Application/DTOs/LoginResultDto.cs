namespace Application.DTOs;

public sealed class LoginResultDto
{
    public string UserType { get; init; } = string.Empty;
    public int UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string TargetPath { get; init; } = string.Empty;
}


