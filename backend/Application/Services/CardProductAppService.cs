// 卡商品应用服务，维护 PRICE_LIST 中的会员卡商品。

using Application.DTOs;
using Application.Helpers;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class CardProductAppService : ICardProductAppService
{
    private readonly IPriceListRepository _priceListRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CardProductAppService(IPriceListRepository priceListRepository, IUnitOfWork unitOfWork)
    {
        _priceListRepository = priceListRepository;
        _unitOfWork = unitOfWork;
    }

    // 会员可见的在售商品
    public async Task<IReadOnlyList<CardProductDto>> GetMembershipProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await _priceListRepository.GetMembershipProductsAsync(cancellationToken);
        return products.Select(MapToDto).ToList();
    }

    // 员工管理列表
    public async Task<IReadOnlyList<CardProductDto>> GetManageListAsync(CancellationToken cancellationToken = default)
    {
        var products = await _priceListRepository.GetManageMembershipProductsAsync(cancellationToken);
        return products.Select(MapToDto).ToList();
    }

    // 新增商品
    public async Task<CardProductDto> CreateAsync(CreateCardProductRequestDto request, CancellationToken cancellationToken = default)
    {
        var productType = request.ProductType.Trim().ToUpperInvariant();
        ValidateProductType(productType);

        if (request.StandardPrice <= 0)
        {
            throw new DomainException("标准价格必须大于 0。");
        }

        var priceId = await _priceListRepository.GetNextPriceIdAsync(cancellationToken);
        var entity = new PriceList
        {
            PriceId = priceId,
            ProductType = productType,
            StandardPrice = request.StandardPrice,
            PriceUpdateTime = DateTime.Today,
        };

        await _priceListRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    // 全量更新（productType 和 standardPrice 都要传）
    public async Task<CardProductDto> UpdateAsync(int priceId, UpdateCardProductRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProductType))
        {
            throw new DomainException("全量更新时 productType 不能为空。");
        }

        if (!request.StandardPrice.HasValue || request.StandardPrice.Value <= 0)
        {
            throw new DomainException("全量更新时 standardPrice 必须大于 0。");
        }

        return await ApplyUpdateAsync(priceId, request, isPatch: false, cancellationToken);
    }

    // 部分更新
    public async Task<CardProductDto> PatchAsync(int priceId, UpdateCardProductRequestDto request, CancellationToken cancellationToken = default)
    {
        return await ApplyUpdateAsync(priceId, request, isPatch: true, cancellationToken);
    }

    // 写入更新逻辑
    private async Task<CardProductDto> ApplyUpdateAsync(
        int priceId,
        UpdateCardProductRequestDto request,
        bool isPatch,
        CancellationToken cancellationToken)
    {
        var entity = await _priceListRepository.GetByIdAsync(priceId, cancellationToken);
        if (entity == null)
        {
            throw new DomainException("未找到编号为 " + priceId + " 的商品。");
        }

        if (!entity.ProductType.Contains("MEMBERSHIP_", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("该记录不是会员卡商品，不能在此维护。");
        }

        if (!isPatch || request.ProductType != null)
        {
            if (string.IsNullOrWhiteSpace(request.ProductType))
            {
                throw new DomainException("productType 不能为空。");
            }

            var normalized = request.ProductType.Trim().ToUpperInvariant();
            ValidateProductType(normalized);
            entity.ProductType = normalized;
        }

        if (!isPatch || request.StandardPrice.HasValue)
        {
            if (!request.StandardPrice.HasValue || request.StandardPrice.Value <= 0)
            {
                throw new DomainException("standardPrice 必须大于 0。");
            }

            entity.StandardPrice = request.StandardPrice.Value;
        }

        if (request.IsActive.HasValue)
        {
            entity.ProductType = request.IsActive.Value
                ? MembershipCardLabels.ActivateProductType(entity.ProductType)
                : MembershipCardLabels.DeactivateProductType(MembershipCardLabels.NormalizeProductType(entity.ProductType));
        }

        entity.PriceUpdateTime = DateTime.Today;
        _priceListRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    // 校验商品编码格式
    private static void ValidateProductType(string productType)
    {
        CardIssuancePolicy.FromProductType(productType);
    }

    // 实体转 DTO
    private static CardProductDto MapToDto(PriceList product)
    {
        var normalizedType = MembershipCardLabels.NormalizeProductType(product.ProductType);
        var cardType = MembershipCardLabels.GetCardTypeFromProductType(normalizedType);

        return new CardProductDto
        {
            PriceId = product.PriceId,
            ProductType = product.ProductType,
            Name = MembershipCardLabels.GetProductDisplayName(normalizedType),
            CardType = cardType,
            Price = product.StandardPrice,
            Description = MembershipCardLabels.GetProductDescription(normalizedType),
            IsActive = MembershipCardLabels.IsActiveProductType(product.ProductType),
        };
    }
}
