using Application.DTOs;

namespace Application.Interfaces;

public interface IGroupCourseAppService
{
    Task<IReadOnlyList<GroupCourseDto>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
