using Domain.Entities;

namespace Domain.Interfaces;

public interface ICoachRepository : IRepository<Coach, int>
{
    Task<IReadOnlyList<Coach>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Coach?> GetByNameAndPhoneAsync(
        string name,
        string phoneNumber,
        CancellationToken cancellationToken = default);
}