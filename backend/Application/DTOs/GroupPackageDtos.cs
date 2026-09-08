namespace Application.DTOs;

public sealed class GroupPackageDto
{
    public int PackageId { get; init; }
    public int MemberId { get; init; }
    public int CourseId { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public int TypeId { get; init; }
    public string CourseTypeName { get; init; } = string.Empty;
    /// <summary>与商品展示名一致，如「瑜伽团课课包·10次」。</summary>
    public string PackageName { get; init; } = string.Empty;
    public string CoachName { get; init; } = string.Empty;
    public int TotalCount { get; init; }
    public int RemainingCount { get; init; }
    public string PackageStatus { get; init; } = string.Empty;
    public string PackageStatusLabel { get; init; } = string.Empty;
    public bool IsUsable { get; init; }
}

public sealed class GroupPackageProductDto
{
    public int PriceId { get; init; }
    public string ProductType { get; init; } = string.Empty;
    public int TypeId { get; init; }
    public string CourseTypeName { get; init; } = string.Empty;
    public int SessionCount { get; init; }
    public decimal Price { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class CreateGroupPackageProductRequestDto
{
    public int TypeId { get; init; }
    public int SessionCount { get; init; }
    public decimal StandardPrice { get; init; }
}

public sealed class UpdateGroupPackageProductRequestDto
{
    public int? SessionCount { get; init; }
    public decimal? StandardPrice { get; init; }
    public bool? IsActive { get; init; }
}

public sealed class PurchaseGroupPackageRequestDto
{
    public int MemberId { get; init; }
    public int PriceId { get; init; }
    /// <summary>可选；不传时由后端按课包课程类型自动选一门团课满足外键。</summary>
    public int? CourseId { get; init; }
    public int? VoucherId { get; init; }
}

public sealed class IssueGroupPackageRequestDto
{
    public int MemberId { get; init; }
    public int PriceId { get; init; }
    public int CourseId { get; init; }
}
