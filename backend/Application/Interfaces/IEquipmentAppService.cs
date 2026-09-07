using Application.DTOs;

namespace Application.Interfaces;

public interface IEquipmentAppService
{
    Task<IReadOnlyList<EquipmentDto>> GetManagementListAsync(string? keyword, string? status, int? venueId, CancellationToken cancellationToken = default);
    Task<EquipmentDto> CreateAsync(CreateEquipmentRequestDto request, CancellationToken cancellationToken = default);
    Task<EquipmentDto> UpdateAsync(int id, UpdateEquipmentRequestDto request, CancellationToken cancellationToken = default);
    Task<UploadEquipmentImageResultDto> SaveImageAsync(string fileName, Stream stream, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
