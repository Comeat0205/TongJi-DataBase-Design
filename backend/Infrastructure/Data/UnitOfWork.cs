using Domain.Interfaces;

namespace Infrastructure.Data;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 统一由工作单元提交，便于后续多个仓储协同保存。
        return _context.SaveChangesAsync(cancellationToken);
    }
}


