using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public sealed class CreateRepairRecordRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "器材编号必须大于 0。")]
    public int EquipId { get; init; }

    [Required(ErrorMessage = "请填写报修问题描述。")]
    [StringLength(200, ErrorMessage = "问题描述不能超过 200 个字符。")]
    public string Description { get; init; } = string.Empty;
}
