using Application.DTOs;

namespace Application.Interfaces;

public interface IVenueAppService
{
    Task<IReadOnlyList<VenueDto>> GetManagementListAsync(string? keyword, string? status, CancellationToken cancellationToken = default);
    Task<VenueDto> CreateAsync(CreateVenueRequestDto request, CancellationToken cancellationToken = default);
    Task<VenueDto> UpdateAsync(int id, UpdateVenueRequestDto request, CancellationToken cancellationToken = default);
    Task<UploadVenueImageResultDto> SaveImageAsync(string fileName, Stream stream, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
