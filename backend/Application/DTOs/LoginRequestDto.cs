namespace Application.DTOs;

public sealed class LoginRequestDto
{
    // member / employee / coach 统一登录入口
    public string LoginType { get; init; } = "member";
    public string LoginName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
