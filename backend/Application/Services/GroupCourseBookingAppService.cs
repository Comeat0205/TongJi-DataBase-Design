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

        // 3. 检查该会员是否已经预约过该课程。
        var exists = await _bookingRepository.ExistsAsync(
            request.MemberId,
            request.CourseId,
            cancellationToken);

        if (exists)
        {
            return (false, null, "该会员已经预约过此课程");
        }

        // 4. 调用 Repository，由 Oracle 存储过程完成实际预约。
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
}
