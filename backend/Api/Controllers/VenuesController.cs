using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class VenuesController : ControllerBase
{
    private readonly IVenueAppService _venueAppService;

    public VenuesController(IVenueAppService venueAppService)
    {
        _venueAppService = venueAppService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VenueDto>>>> GetList(
        [FromQuery] string? keyword,
        [FromQuery] string? status,
        CancellationToken cancellationToken = default)
    {
        var venues = await _venueAppService.GetManagementListAsync(keyword, status, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<VenueDto>>.Success(venues, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<VenueDto>>> Create(
        [FromBody] CreateVenueRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var venue = await _venueAppService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<VenueDto>.Success(venue, HttpContext.TraceIdentifier, "场馆创建成功"));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<VenueDto>>> Update(
        int id,
        [FromBody] UpdateVenueRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var venue = await _venueAppService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<VenueDto>.Success(venue, HttpContext.TraceIdentifier, "场馆更新成功"));
    }

    [HttpPost("upload-image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<UploadVenueImageResultDto>>> UploadImage(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
        {
            return BadRequest(ApiResponse<UploadVenueImageResultDto>.Failure("图片文件不能为空。", HttpContext.TraceIdentifier));
        }

        await using var stream = file.OpenReadStream();
        var result = await _venueAppService.SaveImageAsync(file.FileName, stream, cancellationToken);
        return Ok(ApiResponse<UploadVenueImageResultDto>.Success(result, HttpContext.TraceIdentifier, "图片上传成功"));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken cancellationToken = default)
    {
        await _venueAppService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Success(new { id }, HttpContext.TraceIdentifier, "场馆已删除"));
    }
}
