using Domain.Entities;

namespace Domain.Interfaces;

public interface ICourseTypeRepository : IRepository<CourseType, int>
{
    Task<IReadOnlyList<CourseType>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
