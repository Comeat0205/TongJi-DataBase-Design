using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using System.Text.RegularExpressions;

namespace Application.Services;

public sealed class CoachAppService : ICoachAppService
{
    private static readonly Regex MainlandPhoneRegex = new("^1[3-9]\\d{9}$", RegexOptions.Compiled);
    private static readonly Regex PasswordRegex = new("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d).{8,}$", RegexOptions.Compiled);

    private readonly ICoachRepository _coachRepository;
    private readonly IAppUserRepository _appUserRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CoachAppService(
        ICoachRepository coachRepository,
        IAppUserRepository appUserRepository,
        IUnitOfWork unitOfWork)
    {
        _coachRepository = coachRepository;
        _appUserRepository = appUserRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CoachDto>> GetManagementListAsync(
        string? keyword,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken = default)
    {
        var coaches = await _coachRepository.GetManagementListAsync(keyword, sortBy, sortDirection, cancellationToken);
        return coaches.Select(item => MapToDto(item.Coach, item.User)).ToList();
    }

    public async Task<CoachDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var coach = await _coachRepository.GetByIdAsync(id, cancellationToken);
        if (coach is null || coach.UserId is null)
        {
            return null;
        }

        var user = await _appUserRepository.GetByIdAsync(coach.UserId.Value, cancellationToken);
        if (user is null)
        {
            return null;
        }

        return MapToDto(coach, user);
    }

    public async Task<CoachDto> CreateAsync(CreateCoachRequestDto request, CancellationToken cancellationToken = default)
    {
        var loginName = NormalizeRequired(request.LoginName, "登录名不能为空。");
        var password = request.Password?.Trim() ?? string.Empty;
        var displayName = NormalizeRequired(request.DisplayName, "昵称不能为空。");
        var coachName = NormalizeRequired(request.CoachName, "教练姓名不能为空。");
        var phoneNumber = NormalizeOptional(request.PhoneNumber);
        var sex = NormalizeSex(request.Sex);
        if (sex is null)
        {
            throw new DomainException("请选择性别。");
        }
        var specialty = NormalizeOptional(request.Specialty);
        var coachSummary = NormalizeOptional(request.CoachSummary);
        const string userStatus = "1";
        const string coachStatus = "在职";

        ValidateLengths(loginName, displayName, coachName);
        EnsureValidPassword(password);

        if (phoneNumber is not null)
        {
            EnsureValidPhoneNumber(phoneNumber);
        }

        if (await _appUserRepository.ExistsByLoginNameAsync(loginName, cancellationToken))
        {
            throw new DomainException("登录名已被占用，请更换。");
        }

        if (phoneNumber is not null)
        {
            var existingCoach = await _coachRepository.GetByActivePhoneNumberAsync(phoneNumber, cancellationToken);
            if (existingCoach is not null)
            {
                throw new DomainException("该手机号已被其他教练使用。");
            }
        }

        var userId = await _appUserRepository.GetNextUserIdAsync(cancellationToken);
        var coachId = await _coachRepository.GetNextCoachIdAsync(cancellationToken);
        var now = DateTime.Now;

        var appUser = new AppUser
        {
            UserId = userId,
            LoginName = loginName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            DisplayName = displayName,
            Status = userStatus,
            CreatedAt = now,
            UpdatedAt = now
        };

        var coach = new Coach
        {
            CoachId = coachId,
            UserId = userId,
            CoachName = coachName,
            PhoneNumber = phoneNumber,
            Sex = NormalizeSex(sex),
            Specialty = specialty,
            HireDate = now,
            CoachSummary = coachSummary,
            Status = coachStatus
        };


        await _appUserRepository.AddAsync(appUser, cancellationToken);
        await _coachRepository.AddAsync(coach, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(coach, appUser);
    }

    public async Task<CoachDto> UpdateAsync(int id, UpdateCoachRequestDto request, CancellationToken cancellationToken = default)
    {
        var coach = await _coachRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"未找到编号为 {id} 的教练。");

        if (coach.UserId is null)
        {
            throw new DomainException("当前教练未关联有效的登录账户。");
        }

        var appUser = await _appUserRepository.GetByIdAsync(coach.UserId.Value, cancellationToken)
            ?? throw new DomainException("当前教练未关联有效的登录账户。");

        var displayName = NormalizeRequired(request.DisplayName, "昵称不能为空。");
        var coachName = NormalizeRequired(request.CoachName, "教练姓名不能为空。");
        var phoneNumber = NormalizeOptional(request.PhoneNumber);
        var sex = NormalizeOptional(request.Sex);
        var specialty = NormalizeOptional(request.Specialty);
        var coachSummary = NormalizeOptional(request.CoachSummary);

        ValidateLengths(appUser.LoginName ?? string.Empty, displayName, coachName);

        if (phoneNumber is not null)
        {
            EnsureValidPhoneNumber(phoneNumber);
            var existingCoach = await _coachRepository.GetByActivePhoneNumberAsync(phoneNumber, cancellationToken);
            if (existingCoach is not null && existingCoach.CoachId != coach.CoachId)
            {
                throw new DomainException("该手机号已被其他教练使用。");
            }
        }

        appUser.DisplayName = displayName;
        appUser.UpdatedAt = DateTime.Now;
        coach.CoachName = coachName;
        coach.PhoneNumber = phoneNumber;
        coach.Sex = NormalizeSex(sex);
        coach.Specialty = specialty;
        coach.CoachSummary = coachSummary;

        _appUserRepository.Update(appUser);
        _coachRepository.Update(coach);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(coach, appUser);
    }

    public async Task<CoachDto> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var coach = await _coachRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"未找到编号为 {id} 的教练。");

        if (coach.UserId is null)
        {
            throw new DomainException("当前教练未关联有效的登录账户。");
        }

        var appUser = await _appUserRepository.GetByIdAsync(coach.UserId.Value, cancellationToken)
            ?? throw new DomainException("当前教练未关联有效的登录账户。");

        if (appUser.Status == "0" && NormalizeCoachStatus(coach.Status, "在职") == "离职")
        {
            return MapToDto(coach, appUser);
        }

        appUser.Status = "0";
        appUser.UpdatedAt = DateTime.Now;
        coach.Status = "离职";

        _appUserRepository.Update(appUser);
        _coachRepository.Update(coach);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(coach, appUser);
    }

    private static CoachDto MapToDto(Coach coach, AppUser user)
    {
        return new CoachDto
        {
            CoachId = coach.CoachId,
            UserId = user.UserId,
            DisplayName = ResolveDisplayName(user),
            LoginName = user.LoginName,
            CoachName = coach.CoachName,
            PhoneNumber = coach.PhoneNumber,
            Sex = coach.Sex,
            Specialty = coach.Specialty,
            HireDate = coach.HireDate,
            CoachSummary = coach.CoachSummary,
            Status = NormalizeCoachStatus(coach.Status, "在职")
        };
    }

    private static string ResolveDisplayName(AppUser user)
    {
        return string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.LoginName ?? "教练"
            : user.DisplayName;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeSex(string? value)
    {
        var normalized = NormalizeOptional(value);
        return normalized is "男" or "女" ? normalized : null;
    }

    private static void ValidateLengths(string loginName, string displayName, string coachName)
    {
        if (loginName.Length > 50)
        {
            throw new DomainException("登录名长度不能超过 50 个字符。");
        }

        if (displayName.Length > 50)
        {
            throw new DomainException("昵称长度不能超过 50 个字符。");
        }

        if (coachName.Length > 50)
        {
            throw new DomainException("教练姓名长度不能超过 50 个字符。");
        }
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

    private static string NormalizeRequired(string? value, string message)
    {
        var normalized = NormalizeOptional(value);
        return normalized ?? throw new DomainException(message);
    }

    private static string NormalizeCoachStatus(string? status, string? fallback)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return string.IsNullOrWhiteSpace(fallback) ? "在职" : fallback.Trim();
        }

        return status.Trim() switch
        {
            "在职" => "在职",
            "离职" => "离职",
            _ => throw new DomainException("教练状态只允许为“在职”或“离职”。")
        };
    }
}
