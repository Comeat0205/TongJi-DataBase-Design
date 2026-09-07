using System.Data;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class WaitingQueueRepository
    : Repository<WaitingQueue, int>, IWaitingQueueRepository
{
    public WaitingQueueRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<(bool Success, int QueueId, string Message)> JoinAsync(
        int memberId,
        int courseId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = Context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        // 获取候补队列 ID
        await using var sequenceCommand = connection.CreateCommand();
        sequenceCommand.CommandText =
            "SELECT SEQ_WAITINGQUEUE.NEXTVAL FROM DUAL";
        sequenceCommand.CommandType = CommandType.Text;

        var sequenceResult =
            await sequenceCommand.ExecuteScalarAsync(cancellationToken);

        var queueId = Convert.ToInt32(sequenceResult);

        // 调用 Oracle 存储过程
        await using var command = connection.CreateCommand();
        command.CommandText = "sp_join_waiting_queue";
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

        var queueParameter = command.CreateParameter();
        queueParameter.ParameterName = "p_queue_id";
        queueParameter.DbType = DbType.Int32;
        queueParameter.Direction = ParameterDirection.Input;
        queueParameter.Value = queueId;

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
        command.Parameters.Add(queueParameter);
        command.Parameters.Add(resultParameter);
        command.Parameters.Add(messageParameter);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var success =
            Convert.ToDecimal(resultParameter.Value) == 1;

        var message =
            messageParameter.Value?.ToString() ?? "加入候补失败";

        return (
            success,
            success ? queueId : 0,
            message
        );
    }

    public async Task<IReadOnlyList<WaitingQueue>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        return await Context.WaitingQueues
            .Include(x => x.Course)
            .Where(x =>
                x.MemberId == memberId &&
                x.QueueStatus == "0")
            .OrderBy(x => x.EnqueueTime)
            .ToListAsync(cancellationToken);
    }
}
