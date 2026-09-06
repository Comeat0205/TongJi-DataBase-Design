using Application.DTOs;
using Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/coaches")]
public class CoachesController : ControllerBase
{
    private readonly ICoachRepository _coachRepository;

    public CoachesController(ICoachRepository coachRepository)
    {
        _coachRepository = coachRepository;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<CoachDto>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CoachDto>>>> GetAll(
        CancellationToken cancellationToken = default)
    {
        var coaches = await _coachRepository.GetAllAsync(cancellationToken);

        var data = coaches
            .Where(x => string.IsNullOrWhiteSpace(x.Status) || x.Status == "在职")
            .OrderBy(x => x.CoachId)
            .Select(x => new CoachDto
            {
                CoachId = x.CoachId,
                CoachName = x.CoachName,
                Specialty = x.Specialty,
                Status = x.Status
            })
            .ToList();

        return Ok(
            ApiResponse<IReadOnlyList<CoachDto>>.Success(
                data,
                HttpContext.TraceIdentifier));
    }
}
