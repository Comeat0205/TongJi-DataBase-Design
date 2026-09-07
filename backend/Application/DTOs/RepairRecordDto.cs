namespace Application.DTOs;

public sealed class RepairRecordDto
{
    public int RecordId { get; init; }
    public int EquipId { get; init; }
    public string EquipName { get; init; } = string.Empty;
    public int? EmpId { get; init; }
    public string? EmployeeName { get; init; }
    public DateTime? ReportTime { get; init; }
    public decimal RepairCost { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Description { get; init; }
}
