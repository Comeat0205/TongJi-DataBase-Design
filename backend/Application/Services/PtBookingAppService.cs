using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class PtBookingAppService : IPtBookingAppService
{
    private readonly IPtBookingRepository _ptBookingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PtBookingAppService(
        IPtBookingRepository ptBookingRepository,
        IUnitOfWork unitOfWork)
    {
        _ptBookingRepository = ptBookingRepository;
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

    public async Task<PtBookingDto> BookAsync(
        CreatePtBookingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.MemberId <= 0 || request.PackageId <= 0)
        {
            throw new DomainException("会员编号和课包编号必须有效。");
        }

        if (request.SessionTime <= DateTime.Now)
        {
            throw new DomainException("私教预约时间必须晚于当前时间。");
        }

        var bookingId = await _ptBookingRepository.BookAsync(
            request.MemberId,
            request.PackageId,
            request.SessionTime,
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

        if (booking.CoachConfirmed != "0")
        {
            throw new DomainException("教练已处理该预约，不能再取消。");
        }

        booking.MemberConfirmed = "2";
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
            if (!PersonalTrainingRules.IsPackageUsable(booking.Package, DateTime.Now))
            {
                throw new DomainException("课包已过期、停用或没有剩余次数，无法确认消课。");
            }

            booking.CoachConfirmed = "1";
            booking.Package.RemainingSessions--;
        }
        else
        {
            booking.CoachConfirmed = "2";
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static PtBookingDto MapToDto(Ptbooking booking)
    {
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
            Status = PersonalTrainingRules.GetBookingStatus(booking)
        };
    }
}
