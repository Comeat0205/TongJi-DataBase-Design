namespace Application.DTOs;

public sealed class MemberManagementListItemDto
{
    public int UserId { get; init; }
    public int MemberId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? RealName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? MemberLevel { get; init; }
    public DateTime? RegisterDate { get; init; }
    public string? Status { get; init; }
}
