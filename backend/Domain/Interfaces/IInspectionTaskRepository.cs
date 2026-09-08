using Domain.Entities;

namespace Domain.Interfaces;

public interface IInspectionTaskRepository : IRepository<Inspectiontask, int>
{
    Task<Inspectiontask?> GetDetailsByIdAsync(
        int id,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Inspectiontask>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? status,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, string>> GetVenueOptionsAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, string>> GetEmployeeOptionsAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, string>> GetVenueNamesAsync(
        IEnumerable<int> venueIds,
        CancellationToken cancellationToken = default);
    Task<bool> VenueExistsAsync(int venueId, CancellationToken cancellationToken = default);
    Task<bool> EmployeeExistsAsync(int empId, CancellationToken cancellationToken = default);
    Task<int> GetNextIdAsync(CancellationToken cancellationToken = default);
}
