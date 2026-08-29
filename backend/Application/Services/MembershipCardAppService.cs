// 会员卡应用服务，负责把实体转成 DTO 返回给接口层。

using Application.DTOs;
using Application.Helpers;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public sealed class MembershipCardAppService : IMembershipCardAppService
{
    private readonly IMembershipCardRepository _membershipCardRepository;

    public MembershipCardAppService(IMembershipCardRepository membershipCardRepository)
    {
        _membershipCardRepository = membershipCardRepository;
    }

    // 查某个会员名下的所有卡
    public async Task<IReadOnlyList<MembershipCardDto>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var cards = await _membershipCardRepository.GetByMemberIdAsync(memberId, cancellationToken);
        var result = new List<MembershipCardDto>();

        foreach (var card in cards)
        {
            result.Add(MapToDto(card));
        }

        return result;
    }

    // 按卡编号查一张卡的详情
    public async Task<MembershipCardDto?> GetByIdAsync(int cardId, CancellationToken cancellationToken = default)
    {
        var card = await _membershipCardRepository.GetDetailByIdAsync(cardId, cancellationToken);
        if (card == null)
        {
            return null;
        }

        return MapToDto(card);
    }

    // 把数据库实体转成接口返回用的 DTO
    private static MembershipCardDto MapToDto(MemberBenefitCard card)
    {
        return new MembershipCardDto
        {
            CardId = card.CardId,
            MemberId = card.MemberId,
            CardType = card.CardType,
            CardTypeLabel = MembershipCardLabels.GetCardTypeLabel(card.CardType),
            CardStatus = card.CardStatus,
            IssueDate = card.IssueDate,
            TotalCounts = card.CountCardExtension?.TotalCounts,
            RemainingCount = card.CountCardExtension?.RemainingCount,
            ExpireDate = card.TimeCardExtension?.ExpireDate,
            IsValid = card.IsValidNow()
        };
    }
}
