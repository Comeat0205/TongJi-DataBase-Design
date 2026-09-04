using Domain.Entities;

namespace Domain.Interfaces;

public interface ICoachRepository : IRepository<Coach, int>
{
    // feature/member-template  会员样板模块
    Task<Coach?> GetByNameAndPhoneAsync(string name, string phoneNumber, CancellationToken cancellationToken = default);
    Task<Coach?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    // feature/basic-info  基本信息模块
    Task<Coach?> GetByActivePhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<int> GetNextCoachIdAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(Coach Coach, AppUser User)>> GetManagementListAsync(string? keyword, string? sortBy, string? sortDirection, CancellationToken cancellationToken = default);
}
