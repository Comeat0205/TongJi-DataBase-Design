using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class AuthAppService : IAuthAppService
{
    private readonly IMemberRepository _memberRepository;

    public AuthAppService(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<LoginResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.LoginType, "member", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("当前仅支持会员登录。");
        }

        if (string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            throw new DomainException("请输入用户名/用户ID和手机号。");
        }

        var identifier = request.Identifier.Trim();
        var phoneNumber = request.PhoneNumber.Trim();

        var member = await FindMemberAsync(identifier, phoneNumber, cancellationToken);
        if (member is null)
        {
            throw new DomainException("登录失败，请检查用户名/用户ID和手机号是否匹配。");
        }

        if (member.GetStatus() == MemberStatus.Cancelled)
        {
            throw new DomainException("当前会员状态不可登录，请联系前台处理。");
        }

        return new LoginResultDto
        {
            UserType = "member",
            UserId = member.MemberId,
            DisplayName = member.Name,
            TargetPath = $"/member/profile/{member.MemberId}"
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
}


