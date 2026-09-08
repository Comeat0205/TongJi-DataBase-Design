using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class EquipmentAppService : IEquipmentAppService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly string _imageRootPath;

    public EquipmentAppService(IEquipmentRepository equipmentRepository, IUnitOfWork unitOfWork)
    {
        _equipmentRepository = equipmentRepository;
        _unitOfWork = unitOfWork;
        _imageRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Api", "wwwroot", "uploads", "equipment"));
    }

    public async Task<IReadOnlyList<EquipmentDto>> GetManagementListAsync(string? keyword, string? status, int? venueId, CancellationToken cancellationToken = default)
    {
        var equipmentList = await _equipmentRepository.GetManagementListAsync(keyword, status, venueId, cancellationToken);
        return equipmentList.Select(MapToDto).ToList();
    }

    public async Task<EquipmentDto> CreateAsync(CreateEquipmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var equipName = NormalizeRequired(request.EquipName, "器材名称不能为空。");
        var imageUrl = NormalizeImageUrl(request.ImageUrl);
        ValidateEquipmentName(equipName);

        var equipment = new Equipment
        {
            EquipId = await _equipmentRepository.GetNextEquipmentIdAsync(cancellationToken),
            EquipName = equipName,
            VenueId = request.VenueId,
            ImageUrl = imageUrl,
            PurchaseDate = DateTime.Now,
            Status = "1"
        };

        await _equipmentRepository.AddAsync(equipment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(equipment);
    }

    public async Task<EquipmentDto> UpdateAsync(int id, UpdateEquipmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"未找到编号为 {id} 的器材。");

        var equipName = NormalizeRequired(request.EquipName, "器材名称不能为空。");
        var imageUrl = NormalizeImageUrl(request.ImageUrl);
        var status = NormalizeStatus(request.Status);
        ValidateEquipmentName(equipName);

        if (!string.Equals(equipment.ImageUrl, imageUrl, StringComparison.OrdinalIgnoreCase))
        {
            DeleteImageIfExists(equipment.ImageUrl);
        }

        equipment.EquipName = equipName;
        equipment.VenueId = request.VenueId;
        equipment.ImageUrl = imageUrl;
        equipment.Status = status;

        _equipmentRepository.Update(equipment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(equipment);
    }

    public async Task<UploadEquipmentImageResultDto> SaveImageAsync(string fileName, Stream stream, CancellationToken cancellationToken = default)
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

        var generatedFileName = $"equipment-{DateTime.Now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var targetPath = Path.Combine(_imageRootPath, generatedFileName);

        await using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return new UploadEquipmentImageResultDto
        {
            ImageUrl = $"/uploads/equipment/{generatedFileName}"
        };
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"未找到编号为 {id} 的器材。");

        equipment.Status = "0";
        _equipmentRepository.Update(equipment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static EquipmentDto MapToDto(Equipment equipment)
    {
        return new EquipmentDto
        {
            EquipId = equipment.EquipId,
            EquipName = equipment.EquipName,
            VenueId = equipment.VenueId,
            ImageUrl = equipment.ImageUrl,
            Status = equipment.Status,
            PurchaseDate = equipment.PurchaseDate
        };
    }

    private static void ValidateEquipmentName(string equipName)
    {
        if (equipName.Length > 50)
        {
            throw new DomainException("器材名称长度不能超过 50 个字符。");
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

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeStatus(string? value)
    {
        var normalized = value?.Trim();
        return normalized switch
        {
            "1" => "1",
            "0" => "0",
            _ => throw new DomainException("器材状态只能为 1（正常）或 0（停用）。")
        };
    }

    private void DeleteImageIfExists(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return;
        }

        var normalized = imageUrl.Trim();
        if (!normalized.StartsWith("/uploads/equipment/", StringComparison.OrdinalIgnoreCase))
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
        if (!normalized.StartsWith("/uploads/equipment/", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("器材图片路径不合法。");
        }

        return normalized;
    }
}
