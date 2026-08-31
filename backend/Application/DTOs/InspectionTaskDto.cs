namespace Application.DTOs;

public sealed class InspectionTaskDto
{
    public int TaskId { get; init; }
    public int VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public int EmpId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public DateTime TaskTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Remark { get; init; }
}
