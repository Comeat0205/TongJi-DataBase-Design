namespace Domain.Interfaces;

public interface ICheckInOutRepository : IRepository<Entities.Checkinout, int>
{
    Task<Entities.Checkinout?> GetActiveCheckInAsync(int cardId, int venueId, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Checkinout>> GetActiveCheckInsByVenueAsync(int venueId, CancellationToken ct = default);
    Task<Entities.Checkinout?> GetWithDetailsAsync(int checkInOutId, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Checkinout>> GetPagedAsync(int venueId, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<int> GetNextIdAsync(CancellationToken ct = default);
    Task<Entities.MemberBenefitCard?> GetCardWithDetailsAsync(int cardId, CancellationToken ct = default);
}
