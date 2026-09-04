using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/members/{memberId:int}/personal-packages")]
public sealed class PersonalPackagesController : ControllerBase
{
    private readonly IPersonalPackageAppService _personalPackageAppService;

    public PersonalPackagesController(IPersonalPackageAppService personalPackageAppService)
    {
        _personalPackageAppService = personalPackageAppService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PersonalPackageDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PersonalPackageDto>>>> GetByMemberId(
        int memberId,
        CancellationToken cancellationToken)
    {
        var packages = await _personalPackageAppService.GetByMemberIdAsync(memberId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PersonalPackageDto>>.Success(
            packages,
            HttpContext.TraceIdentifier));
    }
}
