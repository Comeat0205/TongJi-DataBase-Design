// 会员卡应用服务，负责把实体转成 DTO 返回给接口层。

using Application.DTOs;
using Application.Helpers;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class MembershipCardAppService : IMembershipCardAppService
{
    private readonly IMembershipCardRepository _membershipCardRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IPriceListRepository _priceListRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MembershipCardAppService(
        IMembershipCardRepository membershipCardRepository,
        IMemberRepository memberRepository,
        IPriceListRepository priceListRepository,
        IUnitOfWork unitOfWork)
    {
        _membershipCardRepository = membershipCardRepository;
        _memberRepository = memberRepository;
        _priceListRepository = priceListRepository;
        _unitOfWork = unitOfWork;
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

    // 直接发卡
    public Task<MembershipCardDto> CreateAsync(CreateMembershipCardRequestDto request, CancellationToken cancellationToken = default)
    {
        return IssueCardAsync(request.MemberId, request.PriceId, cancellationToken);
    }

    // 模拟支付成功购卡，内部和 CreateAsync 一样
    public Task<MembershipCardDto> PurchaseMockAsync(PurchaseMembershipCardRequestDto request, CancellationToken cancellationToken = default)
    {
        return IssueCardAsync(request.MemberId, request.PriceId, cancellationToken);
    }

    // 发卡核心逻辑，一次 SaveChanges 完成事务
    private async Task<MembershipCardDto> IssueCardAsync(int memberId, int priceId, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken);
        if (member == null)
        {
            throw new DomainException("未找到编号为 " + memberId + " 的会员。");
        }

        if (!member.IsActive())
        {
            throw new DomainException("当前会员状态不可办卡，请联系前台处理。");
        }

        var price = await _priceListRepository.GetByIdAsync(priceId, cancellationToken);
        if (price == null)
        {
            throw new DomainException("未找到编号为 " + priceId + " 的商品。");
        }

        if (!price.ProductType.StartsWith("MEMBERSHIP_", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("该商品不是会员卡类型，无法发卡。");
        }

        if (!MembershipCardLabels.IsActiveProductType(price.ProductType))
        {
            throw new DomainException("该商品已下架，无法购买。");
        }

        var issuePlan = CardIssuancePolicy.FromProductType(MembershipCardLabels.NormalizeProductType(price.ProductType));
        var cardId = await _membershipCardRepository.GetNextCardIdAsync(cancellationToken);
        var issueDate = DateTime.Today;

        var card = new MemberBenefitCard
        {
            CardId = cardId,
            MemberId = memberId,
            IssueDate = issueDate,
            CardStatus = "1",
            CardType = issuePlan.CardType,
        };

        await _membershipCardRepository.AddCardAsync(card, cancellationToken);

        if (issuePlan.CardType == "0")
        {
            var totalCounts = issuePlan.TotalCounts ?? throw new DomainException("次卡商品缺少次数信息。");
            await _membershipCardRepository.AddCountExtensionAsync(new CountCardExtension
            {
                CardId = cardId,
                TotalCounts = totalCounts,
                RemainingCount = totalCounts,
            }, cancellationToken);
        }
        else
        {
            var validDays = issuePlan.ValidDays ?? throw new DomainException("时效卡商品缺少天数信息。");
            await _membershipCardRepository.AddTimeExtensionAsync(new TimeCardExtension
            {
                CardId = cardId,
                ExpireDate = issueDate.AddDays(validDays),
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdCard = await _membershipCardRepository.GetDetailByIdAsync(cardId, cancellationToken);
        if (createdCard == null)
        {
            throw new DomainException("发卡成功但读取新卡失败，请刷新列表。");
        }

        return MapToDto(createdCard);
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
            IsValid = card.IsValidNow(),
        };
    }
}
