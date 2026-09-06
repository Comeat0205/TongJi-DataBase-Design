using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class CourseTypeRepository
    : Repository<CourseType, int>, ICourseTypeRepository
{
    public CourseTypeRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<CourseType>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .OrderBy(x => x.TypeId)
            .ToListAsync(cancellationToken);
    }
}
