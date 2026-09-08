using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public sealed class UpdateRepairRecordStatusRequest
{
    [Required(ErrorMessage = "请选择维修状态。")]
    public string Status { get; init; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "员工编号必须大于 0。")]
    public int? EmpId { get; init; }

    [Range(typeof(decimal), "0", "999999999999999999.99", ErrorMessage = "维修费用不能为负数。")]
    public decimal? RepairCost { get; init; }
}
