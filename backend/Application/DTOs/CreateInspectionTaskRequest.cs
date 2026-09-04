using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public sealed class CreateInspectionTaskRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "场馆编号必须大于 0。")]
    public int VenueId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "员工编号必须大于 0。")]
    public int EmpId { get; init; }

    [Required(ErrorMessage = "请选择巡检时间。")]
    public DateTime? TaskTime { get; init; }

    [StringLength(200, ErrorMessage = "巡检备注不能超过 200 个字符。")]
    public string? Remark { get; init; }
}
