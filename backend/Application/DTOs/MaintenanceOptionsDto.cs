namespace Application.DTOs;

public sealed class MaintenanceOptionDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class RepairRecordOptionsDto
{
    public IReadOnlyList<MaintenanceOptionDto> Equipment { get; init; } = [];
    public IReadOnlyList<MaintenanceOptionDto> Employees { get; init; } = [];
}

public sealed class InspectionTaskOptionsDto
{
    public IReadOnlyList<MaintenanceOptionDto> Venues { get; init; } = [];
    public IReadOnlyList<MaintenanceOptionDto> Employees { get; init; } = [];
}
