using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class EquipmentController : ControllerBase
{
    private readonly IEquipmentAppService _equipmentAppService;

    public EquipmentController(IEquipmentAppService equipmentAppService)
    {
        _equipmentAppService = equipmentAppService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EquipmentDto>>>> GetList(
        [FromQuery] string? keyword,
        [FromQuery] string? status,
        [FromQuery] int? venueId,
        CancellationToken cancellationToken = default)
    {
        var equipment = await _equipmentAppService.GetManagementListAsync(keyword, status, venueId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<EquipmentDto>>.Success(equipment, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EquipmentDto>>> Create(
        [FromBody] CreateEquipmentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var equipment = await _equipmentAppService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<EquipmentDto>.Success(equipment, HttpContext.TraceIdentifier, "器材创建成功"));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<EquipmentDto>>> Update(
        int id,
        [FromBody] UpdateEquipmentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var equipment = await _equipmentAppService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<EquipmentDto>.Success(equipment, HttpContext.TraceIdentifier, "器材更新成功"));
    }

    [HttpPost("upload-image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<UploadEquipmentImageResultDto>>> UploadImage(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
        {
            return BadRequest(ApiResponse<UploadEquipmentImageResultDto>.Failure("图片文件不能为空。", HttpContext.TraceIdentifier));
        }

        await using var stream = file.OpenReadStream();
        var result = await _equipmentAppService.SaveImageAsync(file.FileName, stream, cancellationToken);
        return Ok(ApiResponse<UploadEquipmentImageResultDto>.Success(result, HttpContext.TraceIdentifier, "图片上传成功"));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken cancellationToken = default)
    {
        await _equipmentAppService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Success(new { id }, HttpContext.TraceIdentifier, "器材已删除"));
    }
}
