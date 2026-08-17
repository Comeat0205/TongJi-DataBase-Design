namespace Application.DTOs;

public sealed class LoginRequestDto
{
    // 为后续教练、管理员登录预留统一入口，目前只开放 member。
    public string LoginType { get; init; } = "member";
    public string Identifier { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
}


