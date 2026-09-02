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
        return coaches.Select(item => new CoachDto
        {
            CoachId = item.Coach.CoachId,
            UserId = item.User.UserId,
            DisplayName = ResolveDisplayName(item.User),
            CoachName = item.Coach.CoachName,
            PhoneNumber = item.Coach.PhoneNumber,
            Sex = item.Coach.Sex,
            Specialty = item.Coach.Specialty,
            HireDate = item.Coach.HireDate,
            CoachSummary = item.Coach.CoachSummary,
            Status = item.Coach.Status
        }).ToList();
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

        return new CoachDto
        {
            CoachId = coach.CoachId,
            UserId = user.UserId,
            DisplayName = ResolveDisplayName(user),
            CoachName = coach.CoachName,
            PhoneNumber = coach.PhoneNumber,
            Sex = coach.Sex,
            Specialty = coach.Specialty,
            HireDate = coach.HireDate,
            CoachSummary = coach.CoachSummary,
            Status = coach.Status
        };
    }

    public async Task<CoachDto> CreateAsync(CreateCoachRequestDto request, CancellationToken cancellationToken = default)
    {
        var loginName = NormalizeRequired(request.LoginName, "登录名不能为空。");
        var password = request.Password?.Trim() ?? string.Empty;
        var displayName = NormalizeRequired(request.DisplayName, "昵称不能为空。");
        var coachName = NormalizeRequired(request.CoachName, "教练姓名不能为空。");
        var phoneNumber = NormalizeOptional(request.PhoneNumber);
        var sex = NormalizeOptional(request.Sex);
        var specialty = NormalizeOptional(request.Specialty);
        var coachSummary = NormalizeOptional(request.CoachSummary);
        var hireDate = request.HireDate ?? DateTime.Today;

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
            var existingCoach = await _coachRepository.GetByPhoneNumberAsync(phoneNumber, cancellationToken);
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
            Status = "1",
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
            HireDate = hireDate,
            CoachSummary = coachSummary,
            Status = "在职"
        };

        await _appUserRepository.AddAsync(appUser, cancellationToken);
        await _coachRepository.AddAsync(coach, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CoachDto
        {
            CoachId = coach.CoachId,
            UserId = appUser.UserId,
            DisplayName = ResolveDisplayName(appUser),
            CoachName = coach.CoachName,
            PhoneNumber = coach.PhoneNumber,
            Sex = coach.Sex,
            Specialty = coach.Specialty,
            HireDate = coach.HireDate,
            CoachSummary = coach.CoachSummary,
            Status = coach.Status
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

    private static string NormalizeRequired(string? value, string message)
    {
        var normalized = NormalizeOptional(value);
        return normalized ?? throw new DomainException(message);
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

    private static string? NormalizeSex(string? sex)
    {
        if (string.IsNullOrWhiteSpace(sex))
        {
            return null;
        }

        return sex.Trim().ToUpperInvariant() switch
        {
            "M" or "男" or "MALE" => "男",
            "F" or "女" or "FEMALE" => "女",
            _ => sex.Trim()
        };
    }
}
