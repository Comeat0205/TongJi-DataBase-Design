using Application.DTOs;

namespace Application.Interfaces;

public interface IAuthAppService
{
    Task<LoginResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}


