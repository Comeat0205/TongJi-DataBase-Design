namespace Application.DTOs;

public sealed class MemberDto
{
    public int MemberId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? IdCard { get; init; }
    public string? MemberLevel { get; init; }
    public string? Gender { get; init; }
    public DateTime? Birthday { get; init; }
    public DateTime? RegisterDate { get; init; }
    public string? Status { get; init; }
}


