using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class VenueAppService : IVenueAppService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly IVenueRepository _venueRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly string _imageRootPath;

    public VenueAppService(IVenueRepository venueRepository, IUnitOfWork unitOfWork)
    {
        _venueRepository = venueRepository;
        _unitOfWork = unitOfWork;
        _imageRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Api", "wwwroot", "uploads", "venues"));
    }

    public async Task<IReadOnlyList<VenueDto>> GetManagementListAsync(string? keyword, string? status, CancellationToken cancellationToken = default)
    {
        var venues = await _venueRepository.GetManagementListAsync(keyword, status, cancellationToken);
        return venues.Select(MapToDto).ToList();
    }

    public async Task<VenueDto> CreateAsync(CreateVenueRequestDto request, CancellationToken cancellationToken = default)
    {
        var venueName = NormalizeRequired(request.VenueName, "场馆名称不能为空。");
        var imageUrl = NormalizeImageUrl(request.ImageUrl);
        ValidateVenueName(venueName);
        ValidateCapacity(request.MaxCapacity);

        var venue = new Venue
        {
            VenueId = await _venueRepository.GetNextVenueIdAsync(cancellationToken),
            VenueName = venueName,
            MaxCapacity = request.MaxCapacity,
            CurrentCapacity = 0,
            ImageUrl = imageUrl,
            VenueStatus = "1"
        };

        await _venueRepository.AddAsync(venue, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(venue);
    }

    public async Task<VenueDto> UpdateAsync(int id, UpdateVenueRequestDto request, CancellationToken cancellationToken = default)
    {
        var venue = await _venueRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"未找到编号为 {id} 的场馆。");

        var venueName = NormalizeRequired(request.VenueName, "场馆名称不能为空。");
        var venueStatus = NormalizeVenueStatus(request.VenueStatus);
        var imageUrl = NormalizeImageUrl(request.ImageUrl);

        ValidateVenueName(venueName);
        ValidateCapacity(request.MaxCapacity);

        venue.VenueName = venueName;
        venue.MaxCapacity = request.MaxCapacity;
        venue.ImageUrl = imageUrl;
        venue.VenueStatus = venueStatus;

        if (venue.CurrentCapacity.HasValue && venue.CurrentCapacity.Value > request.MaxCapacity)
        {
            venue.CurrentCapacity = request.MaxCapacity;
        }

        if (venueStatus == "0")
        {
            var equipments = await _unitOfWork.EquipmentRepository.GetManagementListAsync(null, "all", venue.VenueId, cancellationToken);
            foreach (var equipment in equipments)
            {
                equipment.Status = "0";
                _unitOfWork.EquipmentRepository.Update(equipment);
            }
        }

        _venueRepository.Update(venue);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(venue);
    }

    public async Task<UploadVenueImageResultDto> SaveImageAsync(string fileName, Stream stream, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainException("图片文件名不能为空。");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new DomainException("仅支持 jpg、jpeg、png、webp 格式图片。");
        }

        Directory.CreateDirectory(_imageRootPath);

        var generatedFileName = $"venue-{DateTime.Now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var targetPath = Path.Combine(_imageRootPath, generatedFileName);

        await using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return new UploadVenueImageResultDto
        {
            ImageUrl = $"/uploads/venues/{generatedFileName}"
        };
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var venue = await _venueRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"未找到编号为 {id} 的场馆。");

        venue.VenueStatus = "0";
        _venueRepository.Update(venue);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static VenueDto MapToDto(Venue venue)
    {
        return new VenueDto
        {
            VenueId = venue.VenueId,
            VenueName = venue.VenueName,
            MaxCapacity = venue.MaxCapacity,
            CurrentCapacity = venue.CurrentCapacity,
            ImageUrl = venue.ImageUrl,
            VenueStatus = venue.VenueStatus
        };
    }

    private static void ValidateVenueName(string venueName)
    {
        if (venueName.Length > 100)
        {
            throw new DomainException("场馆名称长度不能超过 100 个字符。");
        }
    }

    private static void ValidateCapacity(short maxCapacity)
    {
        if (maxCapacity <= 0)
        {
            throw new DomainException("最大容量必须大于 0。");
        }
    }

    private static string NormalizeRequired(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(message);
        }

        return value.Trim();
    }

    private static string NormalizeVenueStatus(string? value)
    {
        var normalized = value?.Trim();
        return normalized switch
        {
            "1" => "1",
            "0" => "0",
            _ => throw new DomainException("场馆状态只能为 1 或 0。")
        };
    }

    private void DeleteImageIfExists(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return;
        }

        var normalized = imageUrl.Trim();
        if (!normalized.StartsWith("/uploads/venues/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var fileName = Path.GetFileName(normalized);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        var targetPath = Path.Combine(_imageRootPath, fileName);
        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }
    }

    private static string? NormalizeImageUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (!normalized.StartsWith("/uploads/venues/", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("场馆图片路径不合法。");
        }

        return normalized;
    }
}
