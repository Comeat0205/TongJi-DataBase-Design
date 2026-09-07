    using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/course-types")]
public class CourseTypesController : ControllerBase
{
    private readonly ICourseTypeAppService _courseTypeAppService;

    public CourseTypesController(ICourseTypeAppService courseTypeAppService)
    {
        _courseTypeAppService = courseTypeAppService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<CourseTypeDto>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CourseTypeDto>>>> GetAll(
        CancellationToken cancellationToken = default)
    {
        var data = await _courseTypeAppService.GetAllAsync(cancellationToken);

        return Ok(
            ApiResponse<IReadOnlyList<CourseTypeDto>>.Success(
                data,
                HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ApiResponse<CourseTypeDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CourseTypeDto>>> Create(
        [FromBody] CourseTypeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _courseTypeAppService.CreateAsync(
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(
                ApiResponse<CourseTypeDto?>.Failure(
                    "COURSE_TYPE_CREATE_FAILED",
                    result.Message,
                    HttpContext.TraceIdentifier));
        }

        return Ok(
            ApiResponse<CourseTypeDto>.Success(
                result.Data!,
                HttpContext.TraceIdentifier));
    }

    [HttpPut("{typeId:int}")]
    [ProducesResponseType(
        typeof(ApiResponse<CourseTypeDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CourseTypeDto>>> Update(
        int typeId,
        [FromBody] CourseTypeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _courseTypeAppService.UpdateAsync(
            typeId,
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(
                ApiResponse<CourseTypeDto?>.Failure(
                    "COURSE_TYPE_UPDATE_FAILED",
                    result.Message,
                    HttpContext.TraceIdentifier));
        }

        return Ok(
            ApiResponse<CourseTypeDto>.Success(
                result.Data!,
                HttpContext.TraceIdentifier));
    }

    [HttpDelete("{typeId:int}")]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int typeId,
        CancellationToken cancellationToken = default)
    {
        var result = await _courseTypeAppService.DeleteAsync(
            typeId,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(
                ApiResponse<object>.Failure(
                    "COURSE_TYPE_DELETE_FAILED",
                    result.Message,
                    HttpContext.TraceIdentifier));
        }

        return Ok(
            ApiResponse<object>.Success(
                null,
                HttpContext.TraceIdentifier,
                result.Message));
    }
}
