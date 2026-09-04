using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class MemberAppService : IMemberAppService
{
    private readonly IMemberRepository _memberRepository;
    private readonly IAppUserRepository _appUserRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MemberAppService(
        IMemberRepository memberRepository,
        IAppUserRepository appUserRepository,
        IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _appUserRepository = appUserRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MemberDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(id, cancellationToken);
        return member is null ? null : MapToDto(member);
    }

    public async Task<IReadOnlyList<MemberDto>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        // 统一在应用层兜底分页参数，避免控制器和仓储重复写同样的规则。
        pageNumber = pageNumber <= 0 ? PagingConstants.DefaultPageNumber : pageNumber;
        pageSize = pageSize <= 0 ? PagingConstants.DefaultPageSize : Math.Min(pageSize, PagingConstants.MaxPageSize);

        var members = await _memberRepository.GetPagedAsync(pageNumber, pageSize, cancellationToken);
        return members.Select(MapToDto).ToList();
    }

    public async Task<MemberDto> UpdateAsync(int id, UpdateMemberRequestDto request, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"未找到编号为 {id} 的会员。");

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new DomainException("姓名不能为空。");
        }

        var phone = NormalizeOptional(request.PhoneNumber);
        if (!string.IsNullOrEmpty(phone))
        {
            var existingByPhone = await _memberRepository.GetByPhoneAsync(phone, cancellationToken);
            if (existingByPhone is not null && existingByPhone.MemberId != id)
            {
                throw new DomainException("该手机号已被其他会员使用。");
            }
        }

        var idCard = NormalizeOptional(request.IdCard);
        if (!string.IsNullOrEmpty(idCard) && await _memberRepository.ExistsByIdCardAsync(idCard, cancellationToken))
        {
            var current = member.IdCard;
            if (!string.Equals(current, idCard, StringComparison.Ordinal))
            {
                throw new DomainException("该身份证号已被其他会员使用。");
            }
        }

        member.Name = request.Name.Trim();
        member.PhoneNumber = phone;
        member.Gender = NormalizeGender(request.Gender);
        member.Birthday = request.Birthday;
        member.IdCard = idCard;

        _memberRepository.Update(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(member);
    }

    public async Task<MemberDto> RegisterAsync(RegisterMemberRequestDto request, CancellationToken cancellationToken = default)
    {
        var loginName = request.LoginName.Trim();
        var name = request.Name.Trim();
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(loginName) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("登录名、密码和姓名不能为空。");
        }

        if (password.Length < 6)
        {
            throw new DomainException("密码至少 6 位。");
        }

        if (await _appUserRepository.ExistsByLoginNameAsync(loginName, cancellationToken))
        {
            throw new DomainException("登录名已被占用，请更换。");
        }

        var phone = NormalizeOptional(request.PhoneNumber);
        if (!string.IsNullOrEmpty(phone) && await _memberRepository.ExistsByPhoneAsync(phone, cancellationToken))
        {
            throw new DomainException("该手机号已被注册。");
        }

        var idCard = NormalizeOptional(request.IdCard);
        if (!string.IsNullOrEmpty(idCard) && await _memberRepository.ExistsByIdCardAsync(idCard, cancellationToken))
        {
            throw new DomainException("该身份证号已被注册。");
        }

        var now = DateTime.Now;
        var userId = await _appUserRepository.GetNextUserIdAsync(cancellationToken);
        var memberId = await _memberRepository.GetNextMemberIdAsync(cancellationToken);

        var appUser = new AppUser
        {
            UserId = userId,
            LoginName = loginName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            DisplayName = name,
            Status = "1",
            CreatedAt = now,
            UpdatedAt = now
        };

        var member = new Member
        {
            MemberId = memberId,
            Name = name,
            PhoneNumber = phone,
            IdCard = idCard,
            Gender = NormalizeGender(request.Gender),
            Birthday = request.Birthday,
            RegisterDate = now,
            Status = "1",
            UserId = userId,
            MemberLevel = "普通"
        };

        // 样板约定：必须先落库 USERS，再写 MEMBER.USER_ID（库有 FK_MEMBER_USERS）。
        // 若同一次 SaveChanges 同时插入，EF 可能先插 MEMBER，触发 ORA-02291。
        await _appUserRepository.AddAsync(appUser, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _memberRepository.AddAsync(member, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(member);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
        {
            return null;
        }

        return gender.Trim().ToUpperInvariant() switch
        {
            "M" or "男" or "MALE" => "M",
            "F" or "女" or "FEMALE" => "F",
            _ => gender.Trim().Length == 1 ? gender.Trim().ToUpperInvariant() : null
        };
    }

    private static MemberDto MapToDto(Member member)
    {
        // DTO 用来隔离实体本身，避免把导航属性和持久化细节直接暴露给接口层。
        return new MemberDto
        {
            MemberId = member.MemberId,
            Name = member.Name,
            PhoneNumber = member.PhoneNumber,
            IdCard = member.IdCard,
            MemberLevel = member.MemberLevel,
            Gender = member.Gender,
            Birthday = member.Birthday,
            RegisterDate = member.RegisterDate,
            Status = member.Status
        };
    }
}
