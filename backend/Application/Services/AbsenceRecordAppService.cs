using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;

namespace Application.Services;

public sealed class AbsenceRecordAppService : IAbsenceRecordAppService
{
    private readonly IAbsenceRecordRepository _repository;

    public AbsenceRecordAppService(
        IAbsenceRecordRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<AbsenceRecordDto>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        var records = await _repository.GetByMemberIdAsync(
            memberId,
            cancellationToken);

        return records
            .Select(x => new AbsenceRecordDto
            {
                AbsenceId = x.AbsenceId,
                MemberId = x.MemberId,
                BookingId = x.BookingId,
                CourseName = x.Booking.Course.CourseName,
                CourseDate = x.CourseDate,
                AbsenceTime = x.AbsenceTime
            })
            .ToList();
    }
}
