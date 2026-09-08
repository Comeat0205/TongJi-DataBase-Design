using Application.DTOs;
using Application.Helpers;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class PersonalPackageAppService : IPersonalPackageAppService
{
    private readonly IPersonalPackageRepository _personalPackageRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IPriceListRepository _priceListRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PersonalPackageAppService(
        IPersonalPackageRepository personalPackageRepository,
        IMemberRepository memberRepository,
        IPriceListRepository priceListRepository,
        IUnitOfWork unitOfWork)
    {
        _personalPackageRepository = personalPackageRepository;
        _memberRepository = memberRepository;
        _priceListRepository = priceListRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PersonalPackageDto>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        if (memberId <= 0)
        {
            return [];
        }

        var packages = await _personalPackageRepository.GetByMemberIdAsync(memberId, cancellationToken);
        return packages.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<PersonalPackageProductDto>> GetProductsAsync(
        CancellationToken cancellationToken = default)
    {
        var products = await _priceListRepository.GetPersonalPackageProductsAsync(cancellationToken);
        var result = new List<PersonalPackageProductDto>();

        foreach (var product in products)
        {
            PersonalPackageIssuePlan plan;
            try
            {
                plan = PersonalPackageProductLabels.FromProductType(product.ProductType);
            }
            catch (DomainException)
            {
                continue;
            }

            var course = await _personalPackageRepository.GetCourseByIdAsync(plan.PersonalCourseId, cancellationToken);
            if (course is null || course.CoachId is null or <= 0 || course.Coach is null)
            {
                continue;
            }

            result.Add(new PersonalPackageProductDto
            {
                PriceId = product.PriceId,
                ProductType = product.ProductType,
                PersonalCourseId = course.PersonalCourseId,
                CourseName = course.CourseName,
                CourseDescription = course.CourseDescription,
                CoachId = course.CoachId.Value,
                CoachName = course.Coach.CoachName,
                TotalSessions = plan.TotalSessions,
                ValidDays = plan.ValidDays,
                Price = product.StandardPrice,
                IsActive = PersonalPackageProductLabels.IsActiveProductType(product.ProductType),
            });
        }

        return result;
    }

    public async Task<PersonalPackageDto> CreateAsync(
        CreatePersonalPackageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.MemberId <= 0)
        {
            throw new DomainException("请提供有效的会员 ID。");
        }

        if (request.PriceId <= 0)
        {
            throw new DomainException("请选择要购买的私教课包商品。");
        }

        var member = await _memberRepository.GetByIdAsync(request.MemberId, cancellationToken)
            ?? throw new DomainException($"未找到编号为 {request.MemberId} 的会员。");

        if (!member.IsActive())
        {
            throw new DomainException("当前会员状态不可购买课包，请联系前台处理。");
        }

        var price = await _priceListRepository.GetByIdAsync(request.PriceId, cancellationToken)
            ?? throw new DomainException($"未找到编号为 {request.PriceId} 的商品。");

        if (!PersonalPackageProductLabels.IsPersonalPackageProduct(price.ProductType))
        {
            throw new DomainException("该商品不是私教课包类型，无法发放。");
        }

        if (!PersonalPackageProductLabels.IsActiveProductType(price.ProductType))
        {
            throw new DomainException("该商品已下架，无法购买。");
        }

        var plan = PersonalPackageProductLabels.FromProductType(price.ProductType);
        var course = await _personalPackageRepository.GetCourseByIdAsync(plan.PersonalCourseId, cancellationToken)
            ?? throw new DomainException($"未找到编号为 {plan.PersonalCourseId} 的私教课程。");

        if (course.CoachId is null or <= 0)
        {
            throw new DomainException("该私教课程尚未绑定教练，无法发放课包。");
        }

        var packageId = await _personalPackageRepository.GetNextPackageIdAsync(cancellationToken);
        var package = new Personalpackage
        {
            PackageId = packageId,
            MemberId = request.MemberId,
            CoachId = course.CoachId.Value,
            PersonalCourseId = course.PersonalCourseId,
            TotalSessions = plan.TotalSessions,
            RemainingSessions = plan.TotalSessions,
            ExpireDate = DateTime.Today.AddDays(plan.ValidDays),
            PackageStatus = "有效",
        };

        await _personalPackageRepository.AddAsync(package, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _personalPackageRepository.GetDetailByIdAsync(packageId, cancellationToken)
            ?? throw new DomainException("课包发放成功但读取失败，请刷新列表。");
        return MapToDto(created);
    }

    private static PersonalPackageDto MapToDto(Personalpackage package)
    {
        return new PersonalPackageDto
        {
            PackageId = package.PackageId,
            MemberId = package.MemberId,
            CoachId = package.CoachId,
            CoachName = package.Coach.CoachName,
            PersonalCourseId = package.PersonalCourseId,
            CourseName = package.PersonalCourse.CourseName,
            CourseDescription = package.PersonalCourse.CourseDescription,
            TotalSessions = package.TotalSessions,
            RemainingSessions = package.RemainingSessions,
            ExpireDate = package.ExpireDate,
            PackageStatus = package.PackageStatus,
            IsUsable = PersonalTrainingRules.IsPackageUsable(package, DateTime.Now)
        };
    }
}
