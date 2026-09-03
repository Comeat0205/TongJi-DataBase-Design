using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupCoursesController : ControllerBase
{
    private readonly IGroupCourseAppService _groupCourseAppService;

    public GroupCoursesController(IGroupCourseAppService groupCourseAppService)
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
        // Controller 只负责处理 HTTP 请求，不直接操作 Repository 或 DbContext。
        var courses = await _groupCourseAppService.GetAllAsync(cancellationToken);

        return Ok(
            ApiResponse<IReadOnlyList<GroupCourseDto>>.Success(
                courses,
                HttpContext.TraceIdentifier));
    }
}
