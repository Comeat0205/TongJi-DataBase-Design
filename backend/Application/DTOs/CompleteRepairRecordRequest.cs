using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public sealed class CompleteRepairRecordRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "员工编号必须大于 0。")]
    public int? EmpId { get; init; }

    [Range(typeof(decimal), "0", "999999999999999999.99", ErrorMessage = "维修费用不能为负数。")]
    public decimal? RepairCost { get; init; }
}
