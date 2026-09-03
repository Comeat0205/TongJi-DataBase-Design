namespace Domain.Entities;

/// <summary>
/// 登录账号表 USERS：认证与展示名，业务身份通过各表 USER_ID 关联。
/// </summary>
public class AppUser
{
    public int UserId { get; set; }

    public string? LoginName { get; set; }

    public string? PasswordHash { get; set; }

    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    /// <summary>1-有效，0-停用</summary>
    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
