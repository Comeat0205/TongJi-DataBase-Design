using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembersController : ControllerBase
{
    private readonly IMemberAppService _memberAppService;

    public MembersController(IMemberAppService memberAppService)
    {
        _memberAppService = memberAppService;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<MemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MemberDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        // 控制器只负责处理 HTTP 请求，不直接操作仓储或 DbContext。
        var member = await _memberAppService.GetByIdAsync(id, cancellationToken);
        if (member is null)
        {
            return NotFound(ApiResponse<object>.Failure("NOT_FOUND", $"未找到编号为 {id} 的会员。", HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<MemberDto>.Success(member, HttpContext.TraceIdentifier));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MemberDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MemberDto>>>> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        // 分页参数的纠正逻辑放在 Application 层，Controller 保持尽量轻薄。
        var members = await _memberAppService.GetPagedAsync(pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<MemberDto>>.Success(members, HttpContext.TraceIdentifier));
    }
}
