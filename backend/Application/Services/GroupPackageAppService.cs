using Application.DTOs;
using Application.Helpers;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class GroupPackageAppService : IGroupPackageAppService
{
    private readonly IGroupPackageRepository _groupPackageRepository;
    private readonly IPriceListRepository _priceListRepository;
    private readonly ICourseTypeRepository _courseTypeRepository;
    private readonly IGroupcourseRepository _groupcourseRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GroupPackageAppService(
        IGroupPackageRepository groupPackageRepository,
        IPriceListRepository priceListRepository,
        ICourseTypeRepository courseTypeRepository,
        IGroupcourseRepository groupcourseRepository,
        IMemberRepository memberRepository,
        IUnitOfWork unitOfWork)
    {
        _groupPackageRepository = groupPackageRepository;
        _priceListRepository = priceListRepository;
        _courseTypeRepository = courseTypeRepository;
        _groupcourseRepository = groupcourseRepository;
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<GroupPackageDto>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        if (memberId <= 0)
        {
            return [];
        }

        var packages = await _groupPackageRepository.GetByMemberIdAsync(memberId, cancellationToken);
        return packages.Select(MapPackage).ToList();
    }

    public async Task<IReadOnlyList<GroupPackageProductDto>> GetProductsAsync(
        CancellationToken cancellationToken = default)
    {
        var products = await _priceListRepository.GetGroupPackageProductsAsync(cancellationToken);
        var typeNames = await LoadTypeNamesAsync(cancellationToken);
        return products
            .Where(x => GroupPackageLabels.IsActiveProductType(x.ProductType))
            .Select(x => MapProduct(x, typeNames))
            .ToList();
    }

    public async Task<IReadOnlyList<GroupPackageProductDto>> GetManageProductsAsync(
        CancellationToken cancellationToken = default)
    {
        var products = await _priceListRepository.GetManageGroupPackageProductsAsync(cancellationToken);
        var typeNames = await LoadTypeNamesAsync(cancellationToken);
        return products.Select(x => MapProduct(x, typeNames)).ToList();
    }

    public async Task<GroupPackageProductDto> CreateProductAsync(
        CreateGroupPackageProductRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.TypeId <= 0 || request.SessionCount <= 0 || request.StandardPrice <= 0)
        {
            throw new DomainException("请填写有效的课程类型、次数和价格。");
        }

        var courseType = await _courseTypeRepository.GetByIdAsync(request.TypeId, cancellationToken)
            ?? throw new DomainException("课程类型不存在。");

        var productType = GroupPackageLabels.BuildProductType(request.TypeId, request.SessionCount);
        var existing = await _priceListRepository.GetManageGroupPackageProductsAsync(cancellationToken);
        if (existing.Any(x => GroupPackageLabels.NormalizeProductType(x.ProductType)
                .Equals(productType, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException("该课程类型下已存在相同次数的课包商品。");
        }

        var entity = new PriceList
        {
            PriceId = await _priceListRepository.GetNextPriceIdAsync(cancellationToken),
            ProductType = productType,
            StandardPrice = request.StandardPrice,
            PriceUpdateTime = DateTime.Now,
        };

        await _priceListRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapProduct(entity, new Dictionary<int, string> { [courseType.TypeId] = courseType.TypeName });
    }

    public async Task<GroupPackageProductDto> UpdateProductAsync(
        int priceId,
        UpdateGroupPackageProductRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _priceListRepository.GetByIdAsync(priceId, cancellationToken)
            ?? throw new DomainException("未找到该课包商品。");

        if (!GroupPackageLabels.IsGroupPackageProductType(entity.ProductType))
        {
            throw new DomainException("该商品不是团课课包。");
        }

        if (!GroupPackageLabels.TryParse(entity.ProductType, out var typeId, out var sessions))
        {
            throw new DomainException("课包商品编码无效。");
        }

        if (request.SessionCount is > 0)
        {
            sessions = request.SessionCount.Value;
        }

        if (request.StandardPrice is > 0)
        {
            entity.StandardPrice = request.StandardPrice.Value;
        }

        var normalized = GroupPackageLabels.BuildProductType(typeId, sessions);
        if (request.IsActive == false)
        {
            entity.ProductType = GroupPackageLabels.DeactivateProductType(normalized);
        }
        else if (request.IsActive == true)
        {
            entity.ProductType = GroupPackageLabels.ActivateProductType(normalized);
        }
        else if (GroupPackageLabels.IsActiveProductType(entity.ProductType))
        {
            entity.ProductType = normalized;
        }
        else
        {
            entity.ProductType = GroupPackageLabels.DeactivateProductType(normalized);
        }

        entity.PriceUpdateTime = DateTime.Now;
        _priceListRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var typeNames = await LoadTypeNamesAsync(cancellationToken);
        return MapProduct(entity, typeNames);
    }

    public async Task<GroupPackageDto> IssueAsync(
        IssueGroupPackageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.MemberId <= 0 || request.PriceId <= 0 || request.CourseId <= 0)
        {
            throw new DomainException("请提供有效的会员、商品和团课编号。");
        }

        var member = await _memberRepository.GetByIdAsync(request.MemberId, cancellationToken)
            ?? throw new DomainException("会员不存在。");

        if (!member.IsActive())
        {
            throw new DomainException("当前会员状态不可购买课包。");
        }

        var price = await _priceListRepository.GetByIdAsync(request.PriceId, cancellationToken)
            ?? throw new DomainException("课包商品不存在。");

        if (!GroupPackageLabels.IsActiveProductType(price.ProductType)
            || !GroupPackageLabels.TryParse(price.ProductType, out var typeId, out var sessions))
        {
            throw new DomainException("课包商品无效或已下架。");
        }

        var course = await _groupcourseRepository.GetByIdAsync(request.CourseId, cancellationToken)
            ?? throw new DomainException("团课不存在。");

        if (course.TypeId != typeId)
        {
            throw new DomainException("所选团课与课包课程类型不匹配。");
        }

        var package = new GroupPackage
        {
            PackageId = await _groupPackageRepository.GetNextPackageIdAsync(cancellationToken),
            MemberId = request.MemberId,
            CourseId = request.CourseId,
            TotalCount = sessions,
            RemainingCount = sessions,
            PackageStatus = "1",
        };

        await _groupPackageRepository.AddAsync(package, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _groupPackageRepository.GetDetailByIdAsync(package.PackageId, cancellationToken)
            ?? throw new DomainException("课包开通失败。");
        return MapPackage(created);
    }

    private async Task<Dictionary<int, string>> LoadTypeNamesAsync(CancellationToken cancellationToken)
    {
        var types = await _courseTypeRepository.GetAllAsync(cancellationToken);
        return types.ToDictionary(x => x.TypeId, x => x.TypeName);
    }

    private static GroupPackageDto MapPackage(GroupPackage package)
    {
        var usable = package.PackageStatus?.Trim() == "1" && package.RemainingCount > 0;
        var typeId = package.Course?.TypeId ?? 0;
        var typeName = package.Course?.Type?.TypeName ?? string.Empty;
        return new GroupPackageDto
        {
            PackageId = package.PackageId,
            MemberId = package.MemberId,
            CourseId = package.CourseId,
            CourseName = package.Course?.CourseName ?? string.Empty,
            TypeId = typeId,
            CourseTypeName = typeName,
            PackageName = string.IsNullOrWhiteSpace(typeName)
                ? $"团课课包·{package.TotalCount}次"
                : $"{typeName.Trim()}团课课包·{package.TotalCount}次",
            CoachName = package.Course?.Coach?.CoachName ?? string.Empty,
            TotalCount = package.TotalCount,
            RemainingCount = package.RemainingCount,
            PackageStatus = package.PackageStatus?.Trim() ?? string.Empty,
            PackageStatusLabel = GroupPackageLabels.GetPackageStatusLabel(package.PackageStatus),
            IsUsable = usable,
        };
    }

    private static GroupPackageProductDto MapProduct(PriceList price, IReadOnlyDictionary<int, string> typeNames)
    {
        GroupPackageLabels.TryParse(price.ProductType, out var typeId, out var sessions);
        typeNames.TryGetValue(typeId, out var typeName);
        return new GroupPackageProductDto
        {
            PriceId = price.PriceId,
            ProductType = price.ProductType,
            TypeId = typeId,
            CourseTypeName = typeName ?? string.Empty,
            SessionCount = sessions,
            Price = price.StandardPrice,
            Name = GroupPackageLabels.GetDisplayName(price.ProductType, typeName),
            IsActive = GroupPackageLabels.IsActiveProductType(price.ProductType),
        };
    }
}
