using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupCoursesController : ControllerBase
{
    private readonly IGroupCourseAppService _groupCourseAppService;

    public GroupCoursesController(
        IGroupCourseAppService groupCourseAppService)
    {
        _groupCourseAppService = groupCourseAppService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<GroupCourseDto>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GroupCourseDto>>>> GetAll(
        CancellationToken cancellationToken = default)
    {
        var courses = await _groupCourseAppService.GetAllAsync(
            cancellationToken);

        return Ok(
            ApiResponse<IReadOnlyList<GroupCourseDto>>.Success(
                courses,
                HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ApiResponse<GroupCourseDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GroupCourseDto>>> Create(
        [FromBody] GroupCourseRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _groupCourseAppService.CreateAsync(
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(
                ApiResponse<GroupCourseDto?>.Failure(
                    "GROUP_COURSE_CREATE_FAILED",
                    result.Message,
                    HttpContext.TraceIdentifier));
        }

        return Ok(
            ApiResponse<GroupCourseDto>.Success(
                result.Data!,
                HttpContext.TraceIdentifier,
                result.Message));
    }

    [HttpPut("{courseId:int}")]
    [ProducesResponseType(
        typeof(ApiResponse<GroupCourseDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GroupCourseDto>>> Update(
        int courseId,
        [FromBody] GroupCourseRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _groupCourseAppService.UpdateAsync(
            courseId,
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(
                ApiResponse<GroupCourseDto?>.Failure(
                    "GROUP_COURSE_UPDATE_FAILED",
                    result.Message,
                    HttpContext.TraceIdentifier));
        }

        return Ok(
            ApiResponse<GroupCourseDto>.Success(
                result.Data!,
                HttpContext.TraceIdentifier,
                result.Message));
    }

    [HttpDelete("{courseId:int}")]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int courseId,
        CancellationToken cancellationToken = default)
    {
        var result = await _groupCourseAppService.DeleteAsync(
            courseId,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(
                ApiResponse<object>.Failure(
                    "GROUP_COURSE_DELETE_FAILED",
                    result.Message,
                    HttpContext.TraceIdentifier));
        }

        return Ok(
            ApiResponse<object>.Success(
                null,
                HttpContext.TraceIdentifier,
                result.Message));
    }

    [HttpPost("{courseId:int}/schedule/check")]
[ProducesResponseType(
    typeof(ApiResponse<object>),
    StatusCodes.Status200OK)]
public async Task<ActionResult<ApiResponse<object>>> CheckScheduleConflict(
    int courseId,
    [FromBody] GroupCourseScheduleConflictRequestDto request,
    CancellationToken cancellationToken = default)
{
    request.CourseId = courseId;

    var result = await _groupCourseAppService.CheckScheduleConflictAsync(
        request,
        cancellationToken);

    if (!result.Success)
    {
        return BadRequest(
            ApiResponse<object>.Failure(
                "GROUP_COURSE_SCHEDULE_CONFLICT",
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