using Domain.Entities;

namespace Domain.Interfaces;

public interface IGroupcourseRepository : IRepository<Groupcourse, int>
{
    Task<IReadOnlyList<Groupcourse>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
