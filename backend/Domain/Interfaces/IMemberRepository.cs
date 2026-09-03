using Domain.Entities;

namespace Domain.Interfaces;

public interface IMemberRepository : IRepository<Member, int>
{
    Task<Member?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<Member?> GetByNameAndPhoneAsync(string name, string phoneNumber, CancellationToken cancellationToken = default);
    Task<Member?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdCardAsync(string idCard, CancellationToken cancellationToken = default);
    Task<bool> ExistsByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Member>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetNextMemberIdAsync(CancellationToken cancellationToken = default);
}
