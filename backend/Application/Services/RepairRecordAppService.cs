using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public sealed class RepairRecordAppService : IRepairRecordAppService
{
    private readonly IRepairRecordRepository _repairRecordRepository;

    public RepairRecordAppService(IRepairRecordRepository repairRecordRepository)
    {
        _repairRecordRepository = repairRecordRepository;
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

        var records = await _repairRecordRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            status,
            cancellationToken);

        return records.Select(MapToDto).ToList();
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
