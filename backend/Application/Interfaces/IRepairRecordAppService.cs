using Application.DTOs;

namespace Application.Interfaces;

public interface IRepairRecordAppService
{
    Task<IReadOnlyList<RepairRecordDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? status,
        CancellationToken cancellationToken = default);
}
