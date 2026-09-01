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

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<MemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MemberDto>>> Update(
        int id,
        [FromBody] UpdateMemberRequestDto request,
        CancellationToken cancellationToken)
    {
        var member = await _memberAppService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<MemberDto>.Success(member, HttpContext.TraceIdentifier, "档案已更新"));
    }

    [HttpPost("registration/account-validation")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ValidateRegistrationAccount(
        [FromBody] ValidateMemberRegistrationAccountRequestDto request,
        CancellationToken cancellationToken)
    {
        await _memberAppService.ValidateRegistrationAccountAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Success(null, HttpContext.TraceIdentifier, "账号信息校验通过"));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MemberDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<MemberDto>>> Register(
        [FromBody] RegisterMemberRequestDto request,
        CancellationToken cancellationToken)
    {
        // 当前仅开放会员自助注册；员工/教练账号只保留登录，不提供公开注册入口。
        var member = await _memberAppService.RegisterAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = member.MemberId },
            ApiResponse<MemberDto>.Success(member, HttpContext.TraceIdentifier, "会员注册成功"));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<MemberDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MemberDto>>> Cancel(
        int id,
        CancellationToken cancellationToken)
    {
        var member = await _memberAppService.CancelAsync(id, cancellationToken);
        return Ok(ApiResponse<MemberDto>.Success(member, HttpContext.TraceIdentifier, "会员已注销"));
    }
}
