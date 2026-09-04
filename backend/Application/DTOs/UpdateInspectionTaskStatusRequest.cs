using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public sealed class UpdateInspectionTaskStatusRequest
{
    [Required(ErrorMessage = "请选择巡检状态。")]
    [StringLength(50, ErrorMessage = "巡检状态不能超过 50 个字符。")]
    public string Status { get; init; } = string.Empty;

    [StringLength(200, ErrorMessage = "巡检备注不能超过 200 个字符。")]
    public string? Remark { get; init; }
}
