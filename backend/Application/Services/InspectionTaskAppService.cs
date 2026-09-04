using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class InspectionTaskAppService : IInspectionTaskAppService
{
    private static readonly IReadOnlySet<string> SupportedStatuses =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "待执行",
            "进行中",
            "已完成"
        };

    private static readonly IReadOnlyDictionary<string, string> AllowedTransitions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["待执行"] = "进行中",
            ["进行中"] = "已完成"
        };

    private readonly IInspectionTaskRepository _inspectionTaskRepository;
    private readonly IUnitOfWork _unitOfWork;

    public InspectionTaskAppService(
        IInspectionTaskRepository inspectionTaskRepository,
        IUnitOfWork unitOfWork)
    {
        _inspectionTaskRepository = inspectionTaskRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<InspectionTaskOptionsDto> GetOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var venues = await _inspectionTaskRepository.GetVenueOptionsAsync(cancellationToken);
        var employees = await _inspectionTaskRepository.GetEmployeeOptionsAsync(cancellationToken);

        return new InspectionTaskOptionsDto
        {
            Venues = MapOptions(venues),
            Employees = MapOptions(employees)
        };
    }

    public async Task<InspectionTaskDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var task = await _inspectionTaskRepository.GetDetailsByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var venueNames = await _inspectionTaskRepository.GetVenueNamesAsync(
            new[] { task.VenueId },
            cancellationToken);

        return MapToDto(task, venueNames);
    }

    public async Task<IReadOnlyList<InspectionTaskDto>> GetPagedAsync(
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
            throw new DomainException($"不支持巡检状态“{status}”。");
        }

        var tasks = await _inspectionTaskRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            status,
            cancellationToken);
        var venueNames = await _inspectionTaskRepository.GetVenueNamesAsync(
            tasks.Select(task => task.VenueId),
            cancellationToken);

        return tasks.Select(task => MapToDto(task, venueNames)).ToList();
    }

    public async Task<InspectionTaskDto> CreateAsync(
        CreateInspectionTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.TaskTime.HasValue)
        {
            throw new DomainException("请选择巡检时间。");
        }

        if (!await _inspectionTaskRepository.VenueExistsAsync(request.VenueId, cancellationToken))
        {
            throw new KeyNotFoundException($"未找到编号为 {request.VenueId} 的场馆。");
        }

        if (!await _inspectionTaskRepository.EmployeeExistsAsync(request.EmpId, cancellationToken))
        {
            throw new KeyNotFoundException($"未找到编号为 {request.EmpId} 的员工。");
        }

        var task = new Inspectiontask
        {
            TaskId = await _inspectionTaskRepository.GetNextIdAsync(cancellationToken),
            VenueId = request.VenueId,
            EmpId = request.EmpId,
            TaskTime = request.TaskTime.Value,
            Status = "待执行",
            Remark = NormalizeRemark(request.Remark)
        };

        await _inspectionTaskRepository.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await LoadRequiredAsync(task.TaskId, cancellationToken);
    }

    public async Task<InspectionTaskDto> UpdateStatusAsync(
        int id,
        UpdateInspectionTaskStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await _inspectionTaskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"未找到编号为 {id} 的巡检任务。");

        var targetStatus = request.Status.Trim();
        EnsureTransition(task.Status ?? "待执行", targetStatus);

        task.Status = targetStatus;
        if (request.Remark is not null)
        {
            task.Remark = NormalizeRemark(request.Remark);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await LoadRequiredAsync(task.TaskId, cancellationToken);
    }

    private async Task<InspectionTaskDto> LoadRequiredAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var task = await _inspectionTaskRepository.GetDetailsByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"未找到编号为 {id} 的巡检任务。");
        var venueNames = await _inspectionTaskRepository.GetVenueNamesAsync(
            new[] { task.VenueId },
            cancellationToken);

        return MapToDto(task, venueNames);
    }

    private static void EnsureTransition(string currentStatus, string targetStatus)
    {
        if (!SupportedStatuses.Contains(targetStatus))
        {
            throw new DomainException($"不支持巡检状态“{targetStatus}”。");
        }

        if (currentStatus == targetStatus)
        {
            return;
        }

        if (!AllowedTransitions.TryGetValue(currentStatus, out var allowedStatus)
            || allowedStatus != targetStatus)
        {
            throw new DomainException($"巡检状态不能从“{currentStatus}”变更为“{targetStatus}”。");
        }
    }

    private static string? NormalizeRemark(string? remark)
    {
        return string.IsNullOrWhiteSpace(remark) ? null : remark.Trim();
    }

    private static InspectionTaskDto MapToDto(
        Inspectiontask task,
        IReadOnlyDictionary<int, string> venueNames)
    {
        return new InspectionTaskDto
        {
            TaskId = task.TaskId,
            VenueId = task.VenueId,
            VenueName = venueNames.GetValueOrDefault(task.VenueId, "未知场馆"),
            EmpId = task.EmpId,
            EmployeeName = task.Emp.EmpName,
            TaskTime = task.TaskTime,
            Status = task.Status ?? "待执行",
            Remark = task.Remark
        };
    }

    private static IReadOnlyList<MaintenanceOptionDto> MapOptions(
        IReadOnlyDictionary<int, string> options)
    {
        return options
            .OrderBy(option => option.Key)
            .Select(option => new MaintenanceOptionDto
            {
                Id = option.Key,
                Name = option.Value
            })
            .ToList();
    }
}
