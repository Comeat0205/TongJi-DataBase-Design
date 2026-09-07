using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;

namespace Application.Services;

public sealed class GroupCourseBookingAppService
    : IGroupCourseBookingAppService
{
    private readonly IMemberRepository _memberRepository;
    private readonly IGroupcourseRepository _groupcourseRepository;
    private readonly IGroupCourseBookingRepository _bookingRepository;

    public GroupCourseBookingAppService(
        IMemberRepository memberRepository,
        IGroupcourseRepository groupcourseRepository,
        IGroupCourseBookingRepository bookingRepository)
    {
        _memberRepository = memberRepository;
        _groupcourseRepository = groupcourseRepository;
        _bookingRepository = bookingRepository;
    }

    public async Task<(bool Success, GroupCourseBookingDto? Data, string Message)> BookAsync(
    GroupCourseBookingRequestDto request,
    CancellationToken cancellationToken = default)
{
    // 1. 检查会员是否存在。
    var member = await _memberRepository.GetByIdAsync(
        request.MemberId,
        cancellationToken);

    if (member is null)
    {
        return (false, null, "会员不存在");
    }

    // 2. 检查课程是否存在。
    var course = await _groupcourseRepository.GetByIdAsync(
        request.CourseId,
        cancellationToken);

    if (course is null)
    {
        return (false, null, "团课不存在");
    }

    // 3. 调用 Repository，由 Oracle 存储过程统一处理：
    //    - 首次预约
    //    - 重复预约
    //    - 取消后重新预约
    //    - 课程容量
    var result = await _bookingRepository.BookAsync(
        request.MemberId,
        request.CourseId,
        cancellationToken);

    if (!result.Success)
    {
        return (false, null, result.Message);
    }

    var data = new GroupCourseBookingDto
    {
        BookingId = result.BookingId,
        MemberId = request.MemberId,
        CourseId = request.CourseId,
        BookingStatus = "1",
        Message = result.Message
    };

    return (true, data, result.Message);
}

    public async Task<(bool Success, string Message)> CancelAsync(
    int memberId,
    int courseId,
    CancellationToken cancellationToken = default)
{
    // 1. 检查会员是否存在。
    var member = await _memberRepository.GetByIdAsync(
        memberId,
        cancellationToken);

    if (member is null)
    {
        return (false, "会员不存在");
    }

    // 2. 检查课程是否存在。
    var course = await _groupcourseRepository.GetByIdAsync(
        courseId,
        cancellationToken);

    if (course is null)
    {
        return (false, "团课不存在");
    }

    // 3. 调用 Oracle 存储过程完成取消预约。
    var result = await _bookingRepository.CancelAsync(
        memberId,
        courseId,
        cancellationToken);

    return (result.Success, result.Message);
}

    public async Task<IReadOnlyList<GroupCourseBookingDto>> GetByMemberIdAsync(
    int memberId,
    CancellationToken cancellationToken = default)
{
    var bookings = await _bookingRepository.GetByMemberIdAsync(
        memberId,
        cancellationToken);

    return bookings
        .Select(x => new GroupCourseBookingDto
        {
            BookingId = x.BookingId,
            MemberId = x.MemberId,
            CourseId = x.CourseId,
            CourseName = x.Course.CourseName,
            BookingTime = x.BookingTime,
            BookingStatus = x.BookingStatus ?? string.Empty,
            Message = string.Empty
        })
        .ToList();
}
}
