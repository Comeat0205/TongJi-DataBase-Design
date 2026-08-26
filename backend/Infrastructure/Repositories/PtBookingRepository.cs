using System.Data;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace Infrastructure.Repositories;

public sealed class PtBookingRepository : Repository<Ptbooking, int>, IPtBookingRepository
{
    public PtBookingRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Ptbooking>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        return await BookingQuery()
            .AsNoTracking()
            .Where(x => x.MemberId == memberId)
            .OrderByDescending(x => x.SessionTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Ptbooking>> GetPendingByCoachIdAsync(
        int coachId,
        CancellationToken cancellationToken = default)
    {
        return await BookingQuery()
            .AsNoTracking()
            .Where(x =>
                x.CoachId == coachId
                && x.CoachConfirmed == "0"
                && x.MemberConfirmed == "1")
            .OrderBy(x => x.SessionTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<Ptbooking?> GetWithPackageAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        return await BookingQuery()
            .FirstOrDefaultAsync(x => x.PtBookingId == bookingId, cancellationToken);
    }

    public async Task<int> BookAsync(
        int memberId,
        int packageId,
        DateTime sessionTime,
        CancellationToken cancellationToken = default)
    {
        var memberParameter = new OracleParameter("p_member_id", OracleDbType.Int32)
        {
            Value = memberId
        };
        var packageParameter = new OracleParameter("p_package_id", OracleDbType.Int32)
        {
            Value = packageId
        };
        var sessionParameter = new OracleParameter("p_session_time", OracleDbType.Date)
        {
            Value = sessionTime
        };
        var bookingIdParameter = new OracleParameter("p_booking_id", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        var resultParameter = new OracleParameter("p_result", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        var messageParameter = new OracleParameter("p_message", OracleDbType.Varchar2, 400)
        {
            Direction = ParameterDirection.Output
        };

        const string command = """
            BEGIN
                sp_book_personal_training(
                    :p_member_id,
                    :p_package_id,
                    :p_session_time,
                    :p_booking_id,
                    :p_result,
                    :p_message);
            END;
            """;

        await Context.Database.ExecuteSqlRawAsync(
            command,
            [
                memberParameter,
                packageParameter,
                sessionParameter,
                bookingIdParameter,
                resultParameter,
                messageParameter
            ],
            cancellationToken);

        var result = Convert.ToInt32(resultParameter.Value.ToString());
        if (result != 1)
        {
            throw new DomainException(messageParameter.Value?.ToString() ?? "私教预约失败。");
        }

        return Convert.ToInt32(bookingIdParameter.Value.ToString());
    }

    private IQueryable<Ptbooking> BookingQuery()
    {
        return Context.Ptbookings
            .Include(x => x.Coach)
            .Include(x => x.Package)
                .ThenInclude(x => x.PersonalCourse);
    }
}
