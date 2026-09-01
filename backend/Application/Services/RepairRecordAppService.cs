using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class RepairRecordAppService : IRepairRecordAppService
{
    private static readonly IReadOnlySet<string> SupportedStatuses =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "待处理",
            "维修中",
            "已完成"
        };

    private static readonly IReadOnlyDictionary<string, string> AllowedTransitions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["待处理"] = "维修中",
            ["维修中"] = "已完成"
        };

    private readonly IRepairRecordRepository _repairRecordRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RepairRecordAppService(
        IRepairRecordRepository repairRecordRepository,
        IUnitOfWork unitOfWork)
    {
        _repairRecordRepository = repairRecordRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RepairRecordDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var record = await _repairRecordRepository.GetDetailsByIdAsync(id, cancellationToken);
        return record is null ? null : MapToDto(record);
    }

    public async Task<IReadOnlyList<RepairRecordDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? status,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber <= 0 ? PagingConstants.DefaultPageNumber : pageNumber;
        pageSize = pageSize <= 0
            ? PagingConstants.DefaultPageSize
            : Math.Min(pageSize, PagingConstants.MaxPageSize);

        status = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (status is not null && !SupportedStatuses.Contains(status))
        {
            throw new DomainException($"不支持报修状态“{status}”。");
        }

        var records = await _repairRecordRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            status,
            cancellationToken);

        return records.Select(MapToDto).ToList();
    }

    public async Task<RepairRecordDto> CreateAsync(
        CreateRepairRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _repairRecordRepository.EquipmentExistsAsync(request.EquipId, cancellationToken))
        {
            throw new KeyNotFoundException($"未找到编号为 {request.EquipId} 的器材。");
        }

        var record = new Repairrecord
        {
            RecordId = await _repairRecordRepository.GetNextIdAsync(cancellationToken),
            EquipId = request.EquipId,
            Status = "待处理",
            Description = request.Description.Trim()
        };

        await _repairRecordRepository.AddAsync(record, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await LoadRequiredAsync(record.RecordId, cancellationToken);
    }

    public async Task<RepairRecordDto> UpdateStatusAsync(
        int id,
        UpdateRepairRecordStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var record = await _repairRecordRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"未找到编号为 {id} 的报修记录。");

        var targetStatus = request.Status.Trim();
        EnsureTransition(record.Status ?? "待处理", targetStatus);

        if (request.EmpId.HasValue)
        {
            if (!await _repairRecordRepository.EmployeeExistsAsync(request.EmpId.Value, cancellationToken))
            {
                throw new KeyNotFoundException($"未找到编号为 {request.EmpId.Value} 的员工。");
            }

            record.EmpId = request.EmpId.Value;
        }

        if (targetStatus != "待处理" && !record.EmpId.HasValue)
        {
            throw new DomainException("进入维修流程前必须指定维修员工。");
        }

        record.Status = targetStatus;
        if (request.RepairCost.HasValue)
        {
            record.RepairCost = request.RepairCost.Value;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await LoadRequiredAsync(record.RecordId, cancellationToken);
    }

    private async Task<RepairRecordDto> LoadRequiredAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var record = await _repairRecordRepository.GetDetailsByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"未找到编号为 {id} 的报修记录。");

        return MapToDto(record);
    }

    private static void EnsureTransition(string currentStatus, string targetStatus)
    {
        if (!SupportedStatuses.Contains(targetStatus))
        {
            throw new DomainException($"不支持报修状态“{targetStatus}”。");
        }

        if (currentStatus == targetStatus)
        {
            return;
        }

        if (!AllowedTransitions.TryGetValue(currentStatus, out var allowedStatus)
            || allowedStatus != targetStatus)
        {
            throw new DomainException($"报修状态不能从“{currentStatus}”变更为“{targetStatus}”。");
        }
    }

    private static RepairRecordDto MapToDto(Repairrecord record)
    {
        return new RepairRecordDto
        {
            RecordId = record.RecordId,
            EquipId = record.EquipId,
            EquipName = record.Equip.EquipName,
            EmpId = record.EmpId,
            EmployeeName = record.Emp?.EmpName,
            ReportTime = record.ReportTime,
            RepairCost = record.RepairCost ?? 0m,
            Status = record.Status ?? "待处理",
            Description = record.Description
        };
    }
}
