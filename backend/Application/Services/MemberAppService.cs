using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Application.Services;

public sealed class MemberAppService : IMemberAppService
{
    private static readonly Regex MainlandPhoneRegex = new("^1[3-9]\\d{9}$", RegexOptions.Compiled);
    private static readonly Regex PasswordRegex = new("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d).{8,}$", RegexOptions.Compiled);
    private static readonly Regex IdCardRegex = new("^\\d{17}[\\dXx]$", RegexOptions.Compiled);
    private static readonly int[] IdCardWeights = [7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2];
    private static readonly char[] IdCardCheckCodes = ['1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2'];

    private readonly IMemberRepository _memberRepository;
    private readonly IAppUserRepository _appUserRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MemberAppService> _logger;

    public MemberAppService(
        IMemberRepository memberRepository,
        IAppUserRepository appUserRepository,
        IUnitOfWork unitOfWork,
        ILogger<MemberAppService> logger)
    {
        _memberRepository = memberRepository;
        _appUserRepository = appUserRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
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

        var name = NormalizeRequired(request.Name, "姓名不能为空。");

        var phone = NormalizeOptional(request.PhoneNumber);
        if (phone is not null)
        {
            EnsureValidPhoneNumber(phone);

            var existingByPhone = await _memberRepository.GetByPhoneAsync(phone, cancellationToken);
            if (existingByPhone is not null && existingByPhone.MemberId != id)
            {
                throw new DomainException("该手机号已被其他会员使用。");
            }
        }

        var idCard = NormalizeOptional(request.IdCard);
        if (idCard is not null)
        {
            EnsureValidIdCard(idCard);

            if (await _memberRepository.ExistsByIdCardAsync(idCard, cancellationToken))
            {
                var current = member.IdCard;
                if (!string.Equals(current, idCard, StringComparison.Ordinal))
                {
                    throw new DomainException("该身份证号已被其他会员使用。");
                }
            }
        }

        member.Name = name;
        member.PhoneNumber = phone;
        member.Gender = NormalizeGender(request.Gender);
        member.Birthday = request.Birthday;
        member.IdCard = idCard;

        _memberRepository.Update(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(member);
    }

    public async Task ValidateRegistrationAccountAsync(
        ValidateMemberRegistrationAccountRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await ValidateRegistrationAccountCoreAsync(
            request.LoginName,
            request.Password,
            request.PhoneNumber,
            cancellationToken);
    }

    public async Task<MemberDto> RegisterAsync(RegisterMemberRequestDto request, CancellationToken cancellationToken = default)
    {
        var loginName = await ValidateRegistrationAccountCoreAsync(
            request.LoginName,
            request.Password,
            request.PhoneNumber,
            cancellationToken);
        var name = NormalizeRequired(request.Name, "实名认证阶段必须填写姓名。");
        var idCard = NormalizeRequired(request.IdCard, "实名认证阶段必须填写身份证号。").ToUpperInvariant();
        EnsureValidIdCard(idCard);

        if (await _memberRepository.ExistsByIdCardAsync(idCard, cancellationToken))
        {
            throw new DomainException("该身份证号已被注册。");
        }

        var (birthday, gender) = ParseIdentityCard(idCard);
        var now = DateTime.Now;
        var userId = await _appUserRepository.GetNextUserIdAsync(cancellationToken);
        var memberId = await _memberRepository.GetNextMemberIdAsync(cancellationToken);

        var appUser = new AppUser
        {
            UserId = userId,
            LoginName = loginName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DisplayName = loginName,
            Status = "0",
            CreatedAt = now,
            UpdatedAt = now
        };

        var member = new Member
        {
            MemberId = memberId,
            Name = name,
            PhoneNumber = request.PhoneNumber.Trim(),
            IdCard = idCard,
            Gender = gender,
            Birthday = birthday,
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

    public async Task<MemberDto> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"未找到编号为 {id} 的会员。");

        member.Cancel();
        _memberRepository.Update(member);

        if (member.UserId is not null)
        {
            var appUser = await _appUserRepository.GetByIdAsync(member.UserId.Value, cancellationToken);
            if (appUser is not null)
            {
                appUser.Status = "0";
                _appUserRepository.Update(appUser);
            }
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToDto(member);
        }
        catch (Exception ex)
        {
            var detail = BuildOracleErrorMessage(ex);
            _logger.LogError(ex, "会员注销失败，MemberId={MemberId}, UserId={UserId}, OracleDetail={OracleDetail}", id, member.UserId, detail);
            throw new DomainException($"注销失败：{detail}");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeRequired(string? value, string message)
    {
        var normalized = NormalizeOptional(value);
        return normalized ?? throw new DomainException(message);
    }

    private static string BuildOracleErrorMessage(Exception exception)
    {
        var message = exception.ToString();
        var inner = exception.InnerException;

        while (inner is not null)
        {
            var innerMessage = inner.ToString();
            if (innerMessage.Contains("ORA-", StringComparison.OrdinalIgnoreCase))
            {
                message = innerMessage;
                break;
            }

            inner = inner.InnerException;
        }

        var oraIndex = message.IndexOf("ORA-", StringComparison.OrdinalIgnoreCase);
        if (oraIndex >= 0)
        {
            message = message[oraIndex..];
        }

        message = message.Replace(Environment.NewLine, " ").Trim();
        var constraint = ExtractConstraintName(message);

        if (!string.IsNullOrWhiteSpace(constraint))
        {
            return $"数据库约束冲突（{constraint}）：{message}";
        }

        return message;
    }

    private static string? ExtractConstraintName(string message)
    {
        const string prefix = "ORA-02292: 违反完整约束条件 (";
        var startIndex = message.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (startIndex >= 0)
        {
            startIndex += prefix.Length;
            var endIndex = message.IndexOf(')', startIndex);
            if (endIndex > startIndex)
            {
                return message[startIndex..endIndex];
            }
        }

        const string prefixUnique = "ORA-00001: 违反唯一约束条件 (";
        startIndex = message.IndexOf(prefixUnique, StringComparison.OrdinalIgnoreCase);
        if (startIndex >= 0)
        {
            startIndex += prefixUnique.Length;
            var endIndex = message.IndexOf(')', startIndex);
            if (endIndex > startIndex)
            {
                return message[startIndex..endIndex];
            }
        }

        return null;
    }

    private async Task<string> ValidateRegistrationAccountCoreAsync(
        string? loginNameValue,
        string? password,
        string? phoneNumberValue,
        CancellationToken cancellationToken)
    {
        var loginName = NormalizeRequired(loginNameValue, "登录名不能为空。");
        if (loginName.Length > 50)
        {
            throw new DomainException("登录名长度不能超过 50 个字符。");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new DomainException("密码不能为空。");
        }

        EnsureValidPassword(password);

        var phoneNumber = NormalizeRequired(phoneNumberValue, "手机号不能为空。");
        EnsureValidPhoneNumber(phoneNumber);

        if (await _appUserRepository.ExistsByLoginNameAsync(loginName, cancellationToken))
        {
            throw new DomainException("登录名已被占用，请更换。");
        }

        if (await _memberRepository.ExistsByPhoneAsync(phoneNumber, cancellationToken))
        {
            throw new DomainException("该手机号已被注册。");
        }

        return loginName;
    }

    private static void EnsureValidPassword(string password)
    {
        if (!PasswordRegex.IsMatch(password))
        {
            throw new DomainException("密码至少 8 位，且必须同时包含大写字母、小写字母和数字。");
        }
    }

    private static void EnsureValidPhoneNumber(string phoneNumber)
    {
        if (!MainlandPhoneRegex.IsMatch(phoneNumber))
        {
            throw new DomainException("请输入合法的 11 位手机号。");
        }
    }

    private static void EnsureValidIdCard(string idCard)
    {
        if (!IdCardRegex.IsMatch(idCard))
        {
            throw new DomainException("请输入合法的 18 位身份证号。");
        }

        _ = ParseIdentityCard(idCard);
    }

    private static (DateTime birthday, string gender) ParseIdentityCard(string idCard)
    {
        var normalized = idCard.Trim().ToUpperInvariant();
        if (!IdCardRegex.IsMatch(normalized))
        {
            throw new DomainException("请输入合法的 18 位身份证号。");
        }

        if (!DateTime.TryParseExact(
                normalized.Substring(6, 8),
                "yyyyMMdd",
                null,
                System.Globalization.DateTimeStyles.None,
                out var birthday))
        {
            throw new DomainException("身份证号中的出生日期无效。");
        }

        var sum = 0;
        for (var index = 0; index < 17; index++)
        {
            sum += (normalized[index] - '0') * IdCardWeights[index];
        }

        var checkCode = IdCardCheckCodes[sum % 11];
        if (normalized[17] != checkCode)
        {
            throw new DomainException("身份证号校验失败，请检查后重试。");
        }

        var gender = ((normalized[16] - '0') % 2) == 1 ? "M" : "F";
        return (birthday, gender);
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
