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
    private readonly IMemberScheduleRepository _memberScheduleRepository;
    private readonly ICoachScheduleRepository _coachScheduleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PtBookingAppService(
        IPtBookingRepository ptBookingRepository,
        IMemberScheduleRepository memberScheduleRepository,
        ICoachScheduleRepository coachScheduleRepository,
        IUnitOfWork unitOfWork)
    {
        _ptBookingRepository = ptBookingRepository;
        _memberScheduleRepository = memberScheduleRepository;
        _coachScheduleRepository = coachScheduleRepository;
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

            booking.CoachConfirmed = "1";
            await EnsurePtSchedulesAsync(booking, cancellationToken);
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
            memberSchedule.Status = "2";
        }

        var coachSchedule = await _coachScheduleRepository.GetBySourceTrackedAsync(
            PtScheduleType,
            ptBookingId,
            cancellationToken);
        if (coachSchedule is not null)
        {
            coachSchedule.Status = "已取消";
        }
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
