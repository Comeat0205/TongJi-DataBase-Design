namespace Application.DTOs;

public sealed class ValidateMemberRegistrationAccountRequestDto
{
    public string LoginName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
}
