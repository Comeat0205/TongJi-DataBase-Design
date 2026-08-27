using Domain.Entities;

namespace Domain.Interfaces;

public interface IAppUserRepository : IRepository<AppUser, int>
{
    Task<AppUser?> GetByLoginNameAsync(string loginName, CancellationToken cancellationToken = default);

    Task<bool> ExistsByLoginNameAsync(string loginName, CancellationToken cancellationToken = default);

    Task<int> GetNextUserIdAsync(CancellationToken cancellationToken = default);
}
