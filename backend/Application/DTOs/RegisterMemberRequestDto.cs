namespace Application.DTOs;

public sealed class RegisterMemberRequestDto
{
    public string LoginName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Gender { get; init; }
    public DateTime? Birthday { get; init; }
    public string? IdCard { get; init; }
}
