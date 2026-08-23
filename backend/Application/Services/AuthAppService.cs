using Application.DTOs;
using Application.Interfaces;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class AuthAppService : IAuthAppService
{
    private readonly IMemberRepository _memberRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICoachRepository _coachRepository;

    public AuthAppService(
        IMemberRepository memberRepository,
        IEmployeeRepository employeeRepository,
        ICoachRepository coachRepository)
    {
        _memberRepository = memberRepository;
        _employeeRepository = employeeRepository;
        _coachRepository = coachRepository;
    }

    public async Task<LoginResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            throw new DomainException("请输入用户名/用户ID和手机号。");
        }

        var loginType = request.LoginType.Trim().ToLowerInvariant();
        var identifier = request.Identifier.Trim();
        var phoneNumber = request.PhoneNumber.Trim();

        return loginType switch
        {
            "member" => await LoginMemberAsync(identifier, phoneNumber, cancellationToken),
            "employee" => await LoginEmployeeAsync(identifier, phoneNumber, cancellationToken),
            "coach" => await LoginCoachAsync(identifier, phoneNumber, cancellationToken),
            _ => throw new DomainException("不支持的登录类型。")
        };
    }

    private async Task<LoginResultDto> LoginMemberAsync(string identifier, string phoneNumber, CancellationToken cancellationToken)
    {
        var member = await FindMemberAsync(identifier, phoneNumber, cancellationToken);
        if (member is null)
        {
            throw new DomainException("登录失败，请检查用户名/用户ID和手机号是否匹配。");
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

    private async Task<LoginResultDto> LoginEmployeeAsync(string identifier, string phoneNumber, CancellationToken cancellationToken)
    {
        var employee = await FindEmployeeAsync(identifier, phoneNumber, cancellationToken);
        if (employee is null)
        {
            throw new DomainException("登录失败，请检查员工姓名/ID和手机号是否匹配。");
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

    private async Task<LoginResultDto> LoginCoachAsync(string identifier, string phoneNumber, CancellationToken cancellationToken)
    {
        var coach = await FindCoachAsync(identifier, phoneNumber, cancellationToken);
        if (coach is null)
        {
            throw new DomainException("登录失败，请检查教练姓名/ID和手机号是否匹配。");
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

    private async Task<Domain.Entities.Member?> FindMemberAsync(string identifier, string phoneNumber, CancellationToken cancellationToken)
    {
        if (int.TryParse(identifier, out var memberId))
        {
            var memberById = await _memberRepository.GetByIdAsync(memberId, cancellationToken);
            if (memberById is not null && string.Equals(memberById.PhoneNumber, phoneNumber, StringComparison.Ordinal))
            {
                return memberById;
            }
        }

        return await _memberRepository.GetByNameAndPhoneAsync(identifier, phoneNumber, cancellationToken);
    }

    private async Task<Domain.Entities.Employee?> FindEmployeeAsync(string identifier, string phoneNumber, CancellationToken cancellationToken)
    {
        if (int.TryParse(identifier, out var empId))
        {
            var employeeById = await _employeeRepository.GetByIdAsync(empId, cancellationToken);
            if (employeeById is not null && string.Equals(employeeById.Phone, phoneNumber, StringComparison.Ordinal))
            {
                return employeeById;
            }
        }

        return await _employeeRepository.GetByNameAndPhoneAsync(identifier, phoneNumber, cancellationToken);
    }

    private async Task<Domain.Entities.Coach?> FindCoachAsync(string identifier, string phoneNumber, CancellationToken cancellationToken)
    {
        if (int.TryParse(identifier, out var coachId))
        {
            var coachById = await _coachRepository.GetByIdAsync(coachId, cancellationToken);
            if (coachById is not null && string.Equals(coachById.PhoneNumber, phoneNumber, StringComparison.Ordinal))
            {
                return coachById;
            }
        }

        return await _coachRepository.GetByNameAndPhoneAsync(identifier, phoneNumber, cancellationToken);
    }
}
