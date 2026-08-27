using Application.DTOs;
using Application.Interfaces;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class AuthAppService : IAuthAppService
{
    private const string InvalidCredentialsMessage = "账号或密码错误";

    private readonly IAppUserRepository _appUserRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICoachRepository _coachRepository;

    public AuthAppService(
        IAppUserRepository appUserRepository,
        IMemberRepository memberRepository,
        IEmployeeRepository employeeRepository,
        ICoachRepository coachRepository)
    {
        _appUserRepository = appUserRepository;
        _memberRepository = memberRepository;
        _employeeRepository = employeeRepository;
        _coachRepository = coachRepository;
    }

    public async Task<LoginResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.LoginName) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new DomainException(InvalidCredentialsMessage);
        }

        var loginType = request.LoginType.Trim().ToLowerInvariant();
        var loginName = request.LoginName.Trim();

        var appUser = await _appUserRepository.GetByLoginNameAsync(loginName, cancellationToken);
        if (appUser is null || string.IsNullOrWhiteSpace(appUser.PasswordHash))
        {
            throw new DomainException(InvalidCredentialsMessage);
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, appUser.PasswordHash))
        {
            throw new DomainException(InvalidCredentialsMessage);
        }

        if (!string.Equals(appUser.Status?.Trim(), "1", StringComparison.Ordinal))
        {
            throw new DomainException("当前账号已停用，请联系管理员处理。");
        }

        return loginType switch
        {
            "member" => await LoginMemberAsync(appUser, cancellationToken),
            "employee" => await LoginEmployeeAsync(appUser, cancellationToken),
            "coach" => await LoginCoachAsync(appUser, cancellationToken),
            _ => throw new DomainException("不支持的登录类型。")
        };
    }

    private async Task<LoginResultDto> LoginMemberAsync(Domain.Entities.AppUser appUser, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetByUserIdAsync(appUser.UserId, cancellationToken);
        if (member is null)
        {
            throw new DomainException(InvalidCredentialsMessage);
        }

        if (member.GetStatus() == Domain.Enums.MemberStatus.Cancelled)
        {
            throw new DomainException("当前会员状态不可登录，请联系前台处理。");
        }

        return new LoginResultDto
        {
            UserType = "member",
            UserId = member.MemberId,
            DisplayName = member.Name,
            TargetPath = "/member/home"
        };
    }

    private async Task<LoginResultDto> LoginEmployeeAsync(Domain.Entities.AppUser appUser, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByUserIdAsync(appUser.UserId, cancellationToken);
        if (employee is null)
        {
            throw new DomainException(InvalidCredentialsMessage);
        }

        if (employee.Status != "1")
        {
            throw new DomainException("当前员工状态不可登录，请联系管理员处理。");
        }

        return new LoginResultDto
        {
            UserType = "employee",
            UserId = employee.EmpId,
            DisplayName = employee.EmpName,
            TargetPath = "/admin/home"
        };
    }

    private async Task<LoginResultDto> LoginCoachAsync(Domain.Entities.AppUser appUser, CancellationToken cancellationToken)
    {
        var coach = await _coachRepository.GetByUserIdAsync(appUser.UserId, cancellationToken);
        if (coach is null)
        {
            throw new DomainException(InvalidCredentialsMessage);
        }

        if (coach.Status is "0")
        {
            throw new DomainException("当前教练状态不可登录，请联系管理员处理。");
        }

        return new LoginResultDto
        {
            UserType = "coach",
            UserId = coach.CoachId,
            DisplayName = coach.CoachName,
            TargetPath = "/coach/home"
        };
    }
}
