using Application.DTOs;

namespace Application.Interfaces;

public interface IInspectionTaskAppService
{
    Task<InspectionTaskOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default);
    Task<InspectionTaskDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InspectionTaskDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? status,
        CancellationToken cancellationToken = default);
    Task<InspectionTaskDto> CreateAsync(
        CreateInspectionTaskRequest request,
        CancellationToken cancellationToken = default);
    Task<InspectionTaskDto> UpdateStatusAsync(
        int id,
        UpdateInspectionTaskStatusRequest request,
        CancellationToken cancellationToken = default);
}
