using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public sealed class GroupCourseBookingAppService : IGroupCourseBookingAppService
{
    private static readonly TimeSpan CancelWindow = TimeSpan.FromHours(3);

    private readonly IMemberRepository _memberRepository;
    private readonly IGroupcourseRepository _groupcourseRepository;
    private readonly IGroupCourseBookingRepository _bookingRepository;
    private readonly IGroupPackageRepository _groupPackageRepository;
    private readonly IWaitingQueueRepository _waitingQueueRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GroupCourseBookingAppService(
        IMemberRepository memberRepository,
        IGroupcourseRepository groupcourseRepository,
        IGroupCourseBookingRepository bookingRepository,
        IGroupPackageRepository groupPackageRepository,
        IWaitingQueueRepository waitingQueueRepository,
        IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _groupcourseRepository = groupcourseRepository;
        _bookingRepository = bookingRepository;
        _groupPackageRepository = groupPackageRepository;
        _waitingQueueRepository = waitingQueueRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<(bool Success, GroupCourseBookingDto? Data, string Message)> BookAsync(
        GroupCourseBookingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.MemberId <= 0 || request.CourseId <= 0 || request.PackageId <= 0)
        {
            return (false, null, "请提供会员、团课和课包编号。");
        }

        var member = await _memberRepository.GetByIdAsync(request.MemberId, cancellationToken);
        if (member is null)
        {
            return (false, null, "会员不存在");
        }

        var course = await _groupcourseRepository.GetByIdAsync(request.CourseId, cancellationToken);
        if (course is null)
        {
            return (false, null, "团课不存在");
        }

        // 需要时段实例判断是否已过期
        var courseWithSlots = (await _groupcourseRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(c => c.CourseId == request.CourseId)
            ?? course;

        var package = await _groupPackageRepository.GetDetailByIdAsync(request.PackageId, cancellationToken);
        if (package is null || package.MemberId != request.MemberId)
        {
            return (false, null, "课包不存在或不属于当前会员");
        }

        if (package.PackageStatus?.Trim() != "1" || package.RemainingCount <= 0)
        {
            return (false, null, "课包不可用或剩余次数不足");
        }

        // 按课程类型核销：课包关联课的 TYPE_ID 须与目标团课一致
        if (package.Course.TypeId != courseWithSlots.TypeId)
        {
            return (false, null, "该课包不适用于此课程类型");
        }

        var activeOnCourse = await _bookingRepository.GetActiveByMemberAndCourseAsync(
            request.MemberId,
            request.CourseId,
            cancellationToken);
        if (activeOnCourse is not null)
        {
            return (false, null, "您已经预约该课程");
        }

        // 表无 COURSE_ID：用 PACKAGE.COURSE_ID 记录本次预约的团课；同一课包同时只能有一条有效预约
        var packageBookings = await _bookingRepository.GetByMemberIdAsync(request.MemberId, cancellationToken);
        if (packageBookings.Any(x =>
                x.PackageId == request.PackageId && x.BookingStatus?.Trim() == "1"))
        {
            return (false, null, "该课包已有进行中的预约，请先取消后再约其他课");
        }

        var current = courseWithSlots.CurrentCapacity ?? 0;
        if (current >= courseWithSlots.MaxCapacity)
        {
            return (false, null, "课程已满");
        }

        if (request.CourseDate is not null)
        {
            var targetDate = request.CourseDate.Value.Date;
            var slot = courseWithSlots.TimeSlot?.TimeSlotInstances
                .FirstOrDefault(x => x.CourseDate.Date == targetDate);
            if (slot is null)
            {
                return (false, null, "所选日期没有该团课排期");
            }

            if (slot.StartTime <= DateTime.Now)
            {
                return (false, null, "不能预约已开始或已结束的团课");
            }
        }
        else
        {
            var courseStart = ResolveCourseStartTime(courseWithSlots);
            if (courseStart is not null && courseStart.Value <= DateTime.Now)
            {
                return (false, null, "不能预约已开始或已结束的团课");
            }
        }

        // 重新以可跟踪实体加载课程与课包
        var trackedCourse = await _groupcourseRepository.GetByIdAsync(request.CourseId, cancellationToken);
        var trackedPackage = await _groupPackageRepository.GetByIdAsync(request.PackageId, cancellationToken);
        if (trackedCourse is null || trackedPackage is null)
        {
            return (false, null, "团课或课包读取失败");
        }

        var bookingId = await _bookingRepository.GetNextBookingIdAsync(cancellationToken);
        var booking = new GroupCourseBooking
        {
            BookingId = bookingId,
            MemberId = request.MemberId,
            PackageId = request.PackageId,
            BookingTime = DateTime.Now,
            BookingStatus = "1",
        };

        // 按类型核销后，把课包外键切到本次预约的团课，便于取消/容量回写
        trackedPackage.CourseId = request.CourseId;
        trackedPackage.RemainingCount -= 1;
        if (trackedPackage.RemainingCount <= 0)
        {
            trackedPackage.PackageStatus = "0";
        }

        trackedCourse.CurrentCapacity = (short)(current + 1);

        await _bookingRepository.AddAsync(booking, cancellationToken);
        _groupPackageRepository.Update(trackedPackage);
        _groupcourseRepository.Update(trackedCourse);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = await MapBookingAsync(bookingId, cancellationToken);
        return (true, dto, "预约成功，已扣除课包 1 次");
    }

    public async Task<(bool Success, string Message)> CancelAsync(
        int memberId,
        int courseId,
        CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetActiveByMemberAndCourseAsync(
            memberId,
            courseId,
            cancellationToken);

        if (booking is null)
        {
            return (false, "未找到该团课预约");
        }

        return await CancelBookingCoreAsync(booking, cancellationToken);
    }

    public async Task<(bool Success, string Message)> CancelByBookingIdAsync(
        int bookingId,
        int memberId,
        CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetDetailByIdAsync(bookingId, cancellationToken);
        if (booking is null || booking.MemberId != memberId)
        {
            return (false, "未找到该团课预约");
        }

        if (booking.BookingStatus?.Trim() != "1")
        {
            return (false, "当前预约状态不允许取消");
        }

        return await CancelBookingCoreAsync(booking, cancellationToken);
    }

    public async Task<IReadOnlyList<GroupCourseBookingDto>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        var bookings = await _bookingRepository.GetByMemberIdAsync(memberId, cancellationToken);
        var now = DateTime.Now;
        return bookings.Select(x => MapBookingEntity(x, now)).ToList();
    }

    private async Task<(bool Success, string Message)> CancelBookingCoreAsync(
        GroupCourseBooking booking,
        CancellationToken cancellationToken)
    {
        if (booking.BookingStatus?.Trim() != "1")
        {
            return (false, "当前预约状态不允许取消");
        }

        var courseId = booking.Package.CourseId;
        var startTime = ResolveCourseStartTime(booking.Package.Course);
        if (startTime is not null && DateTime.Now > startTime.Value - CancelWindow)
        {
            return (false, "开课前三小时内不可取消预约");
        }

        var trackedBooking = await _bookingRepository.GetByIdAsync(booking.BookingId, cancellationToken)
            ?? booking;
        var trackedPackage = await _groupPackageRepository.GetByIdAsync(booking.PackageId, cancellationToken);
        var trackedCourse = await _groupcourseRepository.GetByIdAsync(courseId, cancellationToken);
        if (trackedPackage is null || trackedCourse is null)
        {
            return (false, "课包或团课不存在");
        }

        var current = trackedCourse.CurrentCapacity ?? 0;
        if (current <= 0)
        {
            return (false, "课程当前人数异常，无法取消预约");
        }

        trackedBooking.BookingStatus = "2";
        trackedPackage.RemainingCount += 1;
        if (trackedPackage.PackageStatus?.Trim() == "0")
        {
            trackedPackage.PackageStatus = "1";
        }

        trackedCourse.CurrentCapacity = (short)(current - 1);

        _bookingRepository.Update(trackedBooking);
        _groupPackageRepository.Update(trackedPackage);
        _groupcourseRepository.Update(trackedCourse);

        var promoteMessage = await TryPromoteWaitingAsync(trackedCourse, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (true, string.IsNullOrWhiteSpace(promoteMessage)
            ? "取消预约成功，已归还课包 1 次"
            : promoteMessage);
    }

    private async Task<string> TryPromoteWaitingAsync(
        Groupcourse course,
        CancellationToken cancellationToken)
    {
        // 候补转正：找到最早候补且有可用同类型课包的会员，扣次并建预约
        while (true)
        {
            var waiting = await _waitingQueueRepository.GetEarliestWaitingAsync(course.CourseId, cancellationToken);
            if (waiting is null)
            {
                return "取消预约成功，已归还课包 1 次";
            }

            var packages = await _groupPackageRepository.GetUsableByMemberAndTypeAsync(
                waiting.MemberId,
                course.TypeId,
                cancellationToken);

            var package = packages.FirstOrDefault();
            if (package is null)
            {
                // 无可用课包：标记跳过该候补（仍占队首会死循环）→ 标为已处理但不转正
                waiting.QueueStatus = "2";
                waiting.Notified = "1";
                _waitingQueueRepository.Update(waiting);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                continue;
            }

            var trackedPackage = await _groupPackageRepository.GetByIdAsync(package.PackageId, cancellationToken);
            if (trackedPackage is null)
            {
                waiting.QueueStatus = "2";
                _waitingQueueRepository.Update(waiting);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                continue;
            }

            var bookingId = await _bookingRepository.GetNextBookingIdAsync(cancellationToken);
            await _bookingRepository.AddAsync(new GroupCourseBooking
            {
                BookingId = bookingId,
                MemberId = waiting.MemberId,
                PackageId = trackedPackage.PackageId,
                BookingTime = DateTime.Now,
                BookingStatus = "1",
            }, cancellationToken);

            trackedPackage.CourseId = course.CourseId;
            trackedPackage.RemainingCount -= 1;
            if (trackedPackage.RemainingCount <= 0)
            {
                trackedPackage.PackageStatus = "0";
            }

            course.CurrentCapacity = (short)((course.CurrentCapacity ?? 0) + 1);
            waiting.QueueStatus = "1";
            waiting.Notified = "1";

            _groupPackageRepository.Update(trackedPackage);
            _groupcourseRepository.Update(course);
            _waitingQueueRepository.Update(waiting);

            return "取消预约成功，已归还课包次数；候补会员已自动转正并扣次";
        }
    }

    private async Task<GroupCourseBookingDto?> MapBookingAsync(int bookingId, CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetDetailByIdAsync(bookingId, cancellationToken);
        return booking is null ? null : MapBookingEntity(booking, DateTime.Now);
    }

    private static GroupCourseBookingDto MapBookingEntity(GroupCourseBooking booking, DateTime now)
    {
        var course = booking.Package?.Course;
        var start = course is null ? null : ResolveCourseStartTime(course);
        var canCancel = booking.BookingStatus?.Trim() == "1"
            && (start is null || now <= start.Value - CancelWindow);

        return new GroupCourseBookingDto
        {
            BookingId = booking.BookingId,
            MemberId = booking.MemberId,
            PackageId = booking.PackageId,
            CourseId = course?.CourseId ?? 0,
            CourseName = course?.CourseName ?? string.Empty,
            TypeId = course?.TypeId ?? 0,
            CourseTypeName = course?.Type?.TypeName ?? string.Empty,
            BookingTime = booking.BookingTime,
            BookingStatus = booking.BookingStatus?.Trim() ?? string.Empty,
            BookingStatusLabel = GetStatusLabel(booking.BookingStatus),
            CourseStartTime = start,
            CanCancel = canCancel,
            Message = string.Empty,
        };
    }

    private static DateTime? ResolveCourseStartTime(Groupcourse course)
    {
        var instances = course.TimeSlot?.TimeSlotInstances;
        if (instances is null || instances.Count == 0)
        {
            return null;
        }

        var now = DateTime.Now;
        // 优先下一场未开始的课；若全部已过则返回最近一场（用于展示/禁止再约）
        var upcoming = instances
            .Where(x => x.StartTime > now)
            .OrderBy(x => x.StartTime)
            .Select(x => (DateTime?)x.StartTime)
            .FirstOrDefault();

        if (upcoming is not null)
        {
            return upcoming;
        }

        return instances
            .OrderByDescending(x => x.StartTime)
            .Select(x => (DateTime?)x.StartTime)
            .FirstOrDefault();
    }

    private static string GetStatusLabel(string? status) => status?.Trim() switch
    {
        "0" => "待确认",
        "1" => "已预约",
        "2" => "已取消",
        "3" => "已完成",
        _ => "未知状态",
    };
}
