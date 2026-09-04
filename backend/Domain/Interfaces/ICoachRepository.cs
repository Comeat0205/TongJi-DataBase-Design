using Domain.Entities;

namespace Domain.Interfaces;

public interface ICoachRepository : IRepository<Coach, int>
{
    Task<Coach?> GetByNameAndPhoneAsync(string name, string phoneNumber, CancellationToken cancellationToken = default);
    Task<Coach?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
}
