namespace Application.DTOs;

public sealed class AtRiskMemberDto
{
    public int MemberId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? MemberLevel { get; init; }
    public DateTime? LastCheckInTime { get; init; }
    public int InactiveDays { get; init; }
    public int UnusedVoucherCount { get; init; }
    public string RiskReason { get; init; } = string.Empty;
}
