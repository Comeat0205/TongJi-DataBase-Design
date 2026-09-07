namespace Application.DTOs;

public sealed class MemberCardDto
{
    public int CardId { get; init; }
    public string CardType { get; init; } = "";
    public string CardStatus { get; init; } = "";
    public string CardTypeName { get; init; } = "";
    public string CardStatusName { get; init; } = "";
    public int? RemainingCount { get; init; }
    public int? TotalCounts { get; init; }
    public string? ExpireDate { get; init; }
    public int? DaysToExpire { get; init; }
}
