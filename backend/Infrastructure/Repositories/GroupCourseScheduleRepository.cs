using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Infrastructure.Repositories;

public sealed class GroupCourseScheduleRepository : IGroupCourseScheduleRepository
{
    private readonly AppDbContext _context;

    public GroupCourseScheduleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message)> CheckConflictAsync(
        int courseId,
        int coachId,
        DateTime courseDate,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.CommandText = "sp_check_group_course_schedule";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(new OracleParameter(
            "p_course_id",
            OracleDbType.Decimal)
        {
            Direction = ParameterDirection.Input,
            Value = courseId
        });

        command.Parameters.Add(new OracleParameter(
            "p_coach_id",
            OracleDbType.Decimal)
        {
            Direction = ParameterDirection.Input,
            Value = coachId
        });

        command.Parameters.Add(new OracleParameter(
            "p_course_date",
            OracleDbType.Date)
        {
            Direction = ParameterDirection.Input,
            Value = courseDate.Date
        });

        command.Parameters.Add(new OracleParameter(
            "p_start_time",
            OracleDbType.TimeStamp)
        {
            Direction = ParameterDirection.Input,
            Value = startTime
        });

        command.Parameters.Add(new OracleParameter(
            "p_end_time",
            OracleDbType.TimeStamp)
        {
            Direction = ParameterDirection.Input,
            Value = endTime
        });

        var resultParameter = new OracleParameter(
            "p_result",
            OracleDbType.Decimal)
        {
            Direction = ParameterDirection.Output
        };

        var messageParameter = new OracleParameter(
            "p_message",
            OracleDbType.Varchar2,
            500)
        {
            Direction = ParameterDirection.Output
        };

        command.Parameters.Add(resultParameter);
        command.Parameters.Add(messageParameter);

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);

        var result = Convert.ToInt32(resultParameter.Value?.ToString() ?? "1");

        var message = messageParameter.Value?.ToString()
                      ?? "排课冲突检测失败";

        return (result == 0, message);
    }
}
