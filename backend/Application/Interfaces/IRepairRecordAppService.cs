using Application.DTOs;

namespace Application.Interfaces;

public interface IRepairRecordAppService
{
    Task<RepairRecordOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default);
    Task<RepairRecordDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RepairRecordDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? status,
        CancellationToken cancellationToken = default);
    Task<RepairRecordDto> CreateAsync(
        CreateRepairRecordRequest request,
        CancellationToken cancellationToken = default);
    Task<RepairRecordDto> UpdateStatusAsync(
        int id,
        UpdateRepairRecordStatusRequest request,
        CancellationToken cancellationToken = default);
}
