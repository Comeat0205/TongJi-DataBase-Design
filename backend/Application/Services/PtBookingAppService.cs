using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class PtBookingAppService : IPtBookingAppService
{
    private const string PtScheduleType = "P";
    private static readonly TimeSpan PtSessionDuration = TimeSpan.FromHours(1);

    private readonly IPtBookingRepository _ptBookingRepository;
    private readonly IPersonalPackageRepository _personalPackageRepository;
    private readonly IMemberScheduleRepository _memberScheduleRepository;
    private readonly ICoachScheduleRepository _coachScheduleRepository;
    private readonly IGroupcourseRepository _groupcourseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PtBookingAppService(
        IPtBookingRepository ptBookingRepository,
        IPersonalPackageRepository personalPackageRepository,
        IMemberScheduleRepository memberScheduleRepository,
        ICoachScheduleRepository coachScheduleRepository,
        IGroupcourseRepository groupcourseRepository,
        IUnitOfWork unitOfWork)
    {
        _ptBookingRepository = ptBookingRepository;
        _personalPackageRepository = personalPackageRepository;
        _memberScheduleRepository = memberScheduleRepository;
        _coachScheduleRepository = coachScheduleRepository;
        _groupcourseRepository = groupcourseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PtBookingDto>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        var bookings = await _ptBookingRepository.GetByMemberIdAsync(memberId, cancellationToken);
        return bookings.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<PtBookingDto>> GetPendingByCoachIdAsync(
        int coachId,
        CancellationToken cancellationToken = default)
    {
        var bookings = await _ptBookingRepository.GetPendingByCoachIdAsync(coachId, cancellationToken);
        return bookings.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<PtBookingDto>> GetByCoachIdAsync(
        int coachId,
        CancellationToken cancellationToken = default)
    {
        var bookings = await _ptBookingRepository.GetByCoachIdAsync(coachId, cancellationToken);
        return bookings.Select(MapToDto).ToList();
    }

    public async Task<PtBookingDto> BookAsync(
        CreatePtBookingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.MemberId <= 0 || request.PackageId <= 0)
        {
            throw new DomainException("会员编号和课包编号必须有效。");
        }

        // 前端按北京时间墙钟提交（无时区后缀），与库内 DATE 一致，用本地时间比较。
        var sessionTime = NormalizeWallClock(request.SessionTime);
        if (sessionTime <= DateTime.Now)
        {
            throw new DomainException("私教预约时间必须晚于当前时间。");
        }

        var package = await _personalPackageRepository.GetDetailByIdAsync(request.PackageId, cancellationToken)
            ?? throw new KeyNotFoundException($"未找到编号为 {request.PackageId} 的私教课包。");

        if (package.MemberId != request.MemberId)
        {
            throw new DomainException("只能使用本人的私教课包预约。");
        }

        var sessionEnd = sessionTime.Add(PtSessionDuration);
        await EnsureCoachSlotFreeForBookingAsync(
            package.CoachId,
            sessionTime,
            sessionEnd,
            cancellationToken);

        var bookingId = await _ptBookingRepository.BookAsync(
            request.MemberId,
            request.PackageId,
            sessionTime,
            cancellationToken);

        var booking = await _ptBookingRepository.GetWithPackageAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"预约成功，但未找到编号为 {bookingId} 的预约记录。");

        return MapToDto(booking);
    }

    public async Task CancelAsync(
        int bookingId,
        int memberId,
        CancellationToken cancellationToken = default)
    {
        var booking = await _ptBookingRepository.GetWithPackageAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"未找到编号为 {bookingId} 的私教预约。");

        if (booking.MemberId != memberId)
        {
            throw new DomainException("只能取消自己的私教预约。");
        }

        if (booking.MemberConfirmed == "2")
        {
            throw new DomainException("该预约已经取消。");
        }

        if (booking.CoachConfirmed == "2")
        {
            throw new DomainException("教练已拒绝该预约，无需再取消。");
        }

        if (PersonalTrainingRules.IsConsumed(booking))
        {
            throw new DomainException("已消课的预约不能取消。");
        }

        if (booking.CoachConfirmed == "0")
        {
            // 待教练确认：随时可取消
        }
        else if (booking.CoachConfirmed == "1"
            && booking.SessionTime - DateTime.Now <= TimeSpan.FromHours(24))
        {
            throw new DomainException("距上课不足 24 小时，无法取消已确认的预约。");
        }
        else if (!PersonalTrainingRules.CanMemberCancel(booking, DateTime.Now))
        {
            throw new DomainException("当前预约状态不可取消。");
        }

        var wasConfirmed = booking.CoachConfirmed == "1";
        booking.MemberConfirmed = "2";
        PersonalTrainingRules.RefundSession(booking.Package);

        if (wasConfirmed)
        {
            await CancelPtSchedulesAsync(booking.PtBookingId, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ConfirmAsync(
        int bookingId,
        ConfirmPtBookingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var booking = await _ptBookingRepository.GetWithPackageAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"未找到编号为 {bookingId} 的私教预约。");

        if (booking.CoachId != request.CoachId)
        {
            throw new DomainException("只能处理分配给自己的私教预约。");
        }

        if (booking.MemberConfirmed != "1")
        {
            throw new DomainException("会员已取消该预约。");
        }

        if (booking.CoachConfirmed != "0")
        {
            throw new DomainException("该预约已经处理，不能重复确认。");
        }

        if (request.Accept)
        {
            if (!PersonalTrainingRules.IsPackageActiveForHeldBooking(booking.Package, DateTime.Now))
            {
                throw new DomainException("课包已过期或停用，无法确认预约。");
            }

            var sessionStart = booking.SessionTime;
            var sessionEnd = sessionStart.Add(PtSessionDuration);

            // 确认前再拦一次：与团课 / 已确认私教冲突则不可确认
            await EnsureCoachSlotFreeForBookingAsync(
                booking.CoachId,
                sessionStart,
                sessionEnd,
                cancellationToken,
                excludeBookingId: booking.PtBookingId);

            booking.CoachConfirmed = "1";
            await EnsurePtSchedulesAsync(booking, cancellationToken);

            // 同时间段其他待确认申请自动驳回并退次
            await RejectOverlappingPendingAsync(
                booking.CoachId,
                sessionStart,
                sessionEnd,
                booking.PtBookingId,
                cancellationToken);
        }
        else
        {
            booking.CoachConfirmed = "2";
            // 提交预约已扣次，拒绝时返还
            PersonalTrainingRules.RefundSession(booking.Package);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ConsumeAsync(
        int bookingId,
        PtBookingCoachActionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var booking = await _ptBookingRepository.GetWithPackageAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"未找到编号为 {bookingId} 的私教预约。");

        if (booking.CoachId != request.CoachId)
        {
            throw new DomainException("只能处理分配给自己的私教预约。");
        }

        if (booking.MemberConfirmed != "1")
        {
            throw new DomainException("会员已取消该预约，不能消课。");
        }

        if (booking.CoachConfirmed != "1")
        {
            throw new DomainException("只有教练已确认的预约才能消课。");
        }

        if (!PersonalTrainingRules.CanConsume(booking, DateTime.Now))
        {
            throw new DomainException("未到上课时间或该预约已经消课。");
        }

        if (!PersonalTrainingRules.IsPackageActiveForHeldBooking(booking.Package, DateTime.Now))
        {
            throw new DomainException("课包已过期或停用，无法消课。");
        }

        // 次数已在提交预约时扣减，消课只标记完成
        booking.ConsumeStatus = "1";
        booking.ConsumedTime = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UndoConsumptionAsync(
        int bookingId,
        PtBookingCoachActionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var booking = await _ptBookingRepository.GetWithPackageAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"未找到编号为 {bookingId} 的私教预约。");

        if (booking.CoachId != request.CoachId)
        {
            throw new DomainException("只能撤销自己处理过的私教消课。");
        }

        if (!PersonalTrainingRules.CanUndoConsumption(booking))
        {
            throw new DomainException("只有已消课的预约才能撤销消课。");
        }

        // 次数仍由该预约占用，撤销消课不返还次数
        booking.ConsumeStatus = "0";
        booking.ConsumedTime = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 预约/确认前：与团课时段、已确认私教、教练日程表占用冲突则禁止。
    /// 不检查其他「待确认」私教（允许多学员同时申请）。
    /// </summary>
    private async Task EnsureCoachSlotFreeForBookingAsync(
        int coachId,
        DateTime sessionStart,
        DateTime sessionEnd,
        CancellationToken cancellationToken,
        int? excludeBookingId = null)
    {
        if (await HasGroupCourseOverlapAsync(coachId, sessionStart, sessionEnd, cancellationToken))
        {
            throw new DomainException("该时段教练已有团操课安排，无法预约私教。");
        }

        if (await _ptBookingRepository.HasConfirmedSessionOverlapAsync(
                coachId,
                sessionStart,
                sessionEnd,
                excludeBookingId,
                cancellationToken))
        {
            throw new DomainException("该时段教练已有已确认的私教课，无法预约。");
        }

        var schedules = await _coachScheduleRepository.GetByCoachIdAsync(coachId, cancellationToken);
        var occupied = schedules.Any(s =>
            !IsCancelledCoachScheduleStatus(s.Status)
            && (excludeBookingId is null
                || !(string.Equals(s.ScheduleType, PtScheduleType, StringComparison.OrdinalIgnoreCase)
                    && s.SourceRecordId == excludeBookingId))
            && Overlaps(s.ScheduleStart, s.ScheduleEnd, sessionStart, sessionEnd));

        if (occupied)
        {
            throw new DomainException("该时段教练日程已被占用，无法预约私教。");
        }
    }

    private async Task<bool> HasGroupCourseOverlapAsync(
        int coachId,
        DateTime sessionStart,
        DateTime sessionEnd,
        CancellationToken cancellationToken)
    {
        var courses = await _groupcourseRepository.GetAllAsync(cancellationToken);
        foreach (var course in courses.Where(c => c.CoachId == coachId))
        {
            var instances = course.TimeSlot?.TimeSlotInstances;
            if (instances is null || instances.Count == 0)
            {
                continue;
            }

            if (instances.Any(slot => Overlaps(slot.StartTime, slot.EndTime, sessionStart, sessionEnd)))
            {
                return true;
            }
        }

        return false;
    }

    private async Task RejectOverlappingPendingAsync(
        int coachId,
        DateTime sessionStart,
        DateTime sessionEnd,
        int acceptedBookingId,
        CancellationToken cancellationToken)
    {
        var pending = await _ptBookingRepository.GetPendingOverlappingTrackedAsync(
            coachId,
            sessionStart,
            sessionEnd,
            acceptedBookingId,
            cancellationToken);

        foreach (var other in pending)
        {
            other.CoachConfirmed = "2";
            PersonalTrainingRules.RefundSession(other.Package);
        }
    }

    /// <summary>
    /// 教练确认后写入会员/教练日程（私教类型 P，默认 1 小时）。
    /// </summary>
    private async Task EnsurePtSchedulesAsync(Ptbooking booking, CancellationToken cancellationToken)
    {
        var start = booking.SessionTime;
        var end = start.Add(PtSessionDuration);
        var date = start.Date;

        var memberSchedule = await _memberScheduleRepository.GetBySourceTrackedAsync(
            PtScheduleType,
            booking.PtBookingId,
            cancellationToken);

        if (memberSchedule is null)
        {
            await _memberScheduleRepository.AddAsync(new MemberSchedule
            {
                ScheduleId = await _memberScheduleRepository.GetNextScheduleIdAsync(cancellationToken),
                MemberId = booking.MemberId,
                ScheduleDate = date,
                ScheduleStart = start,
                ScheduleEnd = end,
                ScheduleType = PtScheduleType,
                SourceRecordId = booking.PtBookingId,
                Status = "0",
            }, cancellationToken);
        }
        else
        {
            memberSchedule.ScheduleDate = date;
            memberSchedule.ScheduleStart = start;
            memberSchedule.ScheduleEnd = end;
            memberSchedule.Status = "0";
        }

        var coachSchedule = await _coachScheduleRepository.GetBySourceTrackedAsync(
            PtScheduleType,
            booking.PtBookingId,
            cancellationToken);

        if (coachSchedule is null)
        {
            await _coachScheduleRepository.AddAsync(new CoachSchedule
            {
                ScheduleId = await _coachScheduleRepository.GetNextScheduleIdAsync(cancellationToken),
                CoachId = booking.CoachId,
                ScheduleDate = date,
                ScheduleStart = start,
                ScheduleEnd = end,
                ScheduleType = PtScheduleType,
                SourceRecordId = booking.PtBookingId,
                Status = "正常",
            }, cancellationToken);
        }
        else
        {
            coachSchedule.ScheduleDate = date;
            coachSchedule.ScheduleStart = start;
            coachSchedule.ScheduleEnd = end;
            coachSchedule.Status = "正常";
        }
    }

    private async Task CancelPtSchedulesAsync(int ptBookingId, CancellationToken cancellationToken)
    {
        var memberSchedule = await _memberScheduleRepository.GetBySourceTrackedAsync(
            PtScheduleType,
            ptBookingId,
            cancellationToken);
        if (memberSchedule is not null)
        {
            // MEMBER_SCHEDULE.STATUS：'0' 待上 / '1' 已上 / '2' 已取消
            memberSchedule.Status = "2";
        }

        var coachSchedule = await _coachScheduleRepository.GetBySourceTrackedAsync(
            PtScheduleType,
            ptBookingId,
            cancellationToken);
        if (coachSchedule is not null)
        {
            // COACH_SCHEDULE.STATUS 检查约束仅允许「正常」「已完成」，不能写「已取消」；
            // 取消已确认预约时直接删除教练日程，避免 ORA-02290（如 SYS_C008638）。
            _coachScheduleRepository.Remove(coachSchedule);
        }
    }

    private static bool Overlaps(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
        => aStart < bEnd && bStart < aEnd;

    private static bool IsCancelledCoachScheduleStatus(string? status)
    {
        var s = status?.Trim();
        return string.Equals(s, "已取消", StringComparison.Ordinal)
            || string.Equals(s, "2", StringComparison.Ordinal);
    }

    private static DateTime NormalizeWallClock(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value.ToLocalTime(),
            DateTimeKind.Local => value,
            _ => DateTime.SpecifyKind(value, DateTimeKind.Unspecified)
        };
    }

    private static PtBookingDto MapToDto(Ptbooking booking)
    {
        var now = DateTime.Now;

        return new PtBookingDto
        {
            PtBookingId = booking.PtBookingId,
            PackageId = booking.PackageId,
            MemberId = booking.MemberId,
            CoachId = booking.CoachId,
            CoachName = booking.Coach.CoachName,
            CourseName = booking.Package.PersonalCourse.CourseName,
            BookingTime = booking.BookingTime,
            SessionTime = booking.SessionTime,
            CoachConfirmed = booking.CoachConfirmed,
            MemberConfirmed = booking.MemberConfirmed,
            ConsumeStatus = booking.ConsumeStatus ?? "0",
            ConsumedTime = booking.ConsumedTime,
            Status = PersonalTrainingRules.GetBookingStatus(booking),
            IsConsumed = PersonalTrainingRules.IsConsumed(booking),
            CanConsume = PersonalTrainingRules.CanConsume(booking, now),
            CanUndoConsumption = PersonalTrainingRules.CanUndoConsumption(booking),
            CanCancel = PersonalTrainingRules.CanMemberCancel(booking, now)
        };
    }
}
