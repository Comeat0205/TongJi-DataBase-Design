using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public sealed class PersonalPackageAppService : IPersonalPackageAppService
{
    private readonly IPersonalPackageRepository _personalPackageRepository;

    public PersonalPackageAppService(IPersonalPackageRepository personalPackageRepository)
    {
        _personalPackageRepository = personalPackageRepository;
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
