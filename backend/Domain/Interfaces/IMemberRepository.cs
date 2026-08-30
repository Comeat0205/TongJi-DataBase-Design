using Domain.Entities;

namespace Domain.Interfaces;

public interface IMemberRepository : IRepository<Member, int>
{
    Task<Member?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<Member?> GetByNameAndPhoneAsync(string name, string phoneNumber, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdCardAsync(string idCard, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Member>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Member>> GetActiveMembersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Member>> GetMembersWithBirthdayTodayAsync(CancellationToken cancellationToken = default);
}


