using System.Data;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class GroupCourseBookingRepository
    : Repository<GroupCourseBooking, int>, IGroupCourseBookingRepository
{
    public GroupCourseBookingRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<bool> ExistsAsync(
    int memberId,
    int courseId,
    CancellationToken cancellationToken = default)
{
    var booking = await DbSet
        .AsNoTracking()
        .Where(x => x.MemberId == memberId && x.CourseId == courseId)
        .Select(x => x.BookingId)
        .FirstOrDefaultAsync(cancellationToken);

    return booking != 0;
}
    public async Task<(bool Success, int BookingId, string Message)> BookAsync(
        int memberId,
        int courseId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = Context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        // 先从 Oracle Sequence 获取新的预约编号。
        await using var sequenceCommand = connection.CreateCommand();
        sequenceCommand.CommandText =
            "SELECT SEQ_GROUP_COURSE_BOOKING.NEXTVAL FROM DUAL";
        sequenceCommand.CommandType = CommandType.Text;

        var sequenceResult =
            await sequenceCommand.ExecuteScalarAsync(cancellationToken);

        var bookingId = Convert.ToInt32(sequenceResult);

        // 调用已有的 Oracle 存储过程完成实际预约。
        await using var command = connection.CreateCommand();
        command.CommandText = "sp_book_group_course";
        command.CommandType = CommandType.StoredProcedure;

        var memberParameter = command.CreateParameter();
        memberParameter.ParameterName = "p_member_id";
        memberParameter.DbType = DbType.Int32;
        memberParameter.Direction = ParameterDirection.Input;
        memberParameter.Value = memberId;

        var courseParameter = command.CreateParameter();
        courseParameter.ParameterName = "p_course_id";
        courseParameter.DbType = DbType.Int32;
        courseParameter.Direction = ParameterDirection.Input;
        courseParameter.Value = courseId;

        var bookingParameter = command.CreateParameter();
        bookingParameter.ParameterName = "p_booking_id";
        bookingParameter.DbType = DbType.Int32;
        bookingParameter.Direction = ParameterDirection.Input;
        bookingParameter.Value = bookingId;

        var resultParameter = command.CreateParameter();
        resultParameter.ParameterName = "p_result";
        resultParameter.DbType = DbType.Decimal;
        resultParameter.Direction = ParameterDirection.Output;

        var messageParameter = command.CreateParameter();
        messageParameter.ParameterName = "p_message";
        messageParameter.DbType = DbType.String;
        messageParameter.Size = 4000;
        messageParameter.Direction = ParameterDirection.Output;

        command.Parameters.Add(memberParameter);
        command.Parameters.Add(courseParameter);
        command.Parameters.Add(bookingParameter);
        command.Parameters.Add(resultParameter);
        command.Parameters.Add(messageParameter);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var success =
            Convert.ToDecimal(resultParameter.Value) == 1;

        var message =
            messageParameter.Value?.ToString() ?? "预约失败";

        return (success, success ? bookingId : 0, message);
    }
}