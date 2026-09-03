using Domain.Entities;

namespace Domain.Interfaces;

public interface IEmployeeRepository : IRepository<Employee, int>
{
    Task<Employee?> GetByNameAndPhoneAsync(string name, string phone, CancellationToken cancellationToken = default);
    Task<Employee?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
}
