#:package Oracle.ManagedDataAccess.Core@23.26.0
using System.Text.Json;
using Oracle.ManagedDataAccess.Client;

// 为已确认但尚未写入日程的私教预约补齐 MEMBER_SCHEDULE / COACH_SCHEDULE。

var root = Directory.GetCurrentDirectory();
if (!File.Exists(Path.Combine(root, "backend", "Api", "appsettings.Local.json")))
    root = Path.GetFullPath(Path.Combine(root, ".."));

var json = await File.ReadAllTextAsync(Path.Combine(root, "backend", "Api", "appsettings.Local.json"));
using var doc = JsonDocument.Parse(json);
var cs = doc.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()!;

await using var conn = new OracleConnection(cs);
await conn.OpenAsync();

async Task<decimal> Scalar(string sql)
{
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    return Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0);
}

async Task Exec(string sql)
{
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    await cmd.ExecuteNonQueryAsync();
}

await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = """
        SELECT b.PT_BOOKING_ID, b.MEMBER_ID, b.COACH_ID, b.SESSION_TIME
        FROM PTBOOKING b
        WHERE b.MEMBER_CONFIRMED = '1'
          AND b.COACH_CONFIRMED = '1'
          AND NOT EXISTS (
              SELECT 1 FROM MEMBER_SCHEDULE m
              WHERE m.SCHEDULE_TYPE = 'P' AND m.SOURCE_RECORD_ID = b.PT_BOOKING_ID
          )
        ORDER BY b.PT_BOOKING_ID
        """;

    await using var reader = await cmd.ExecuteReaderAsync();
    var rows = new List<(int Id, int MemberId, int CoachId, DateTime Session)>();
    while (await reader.ReadAsync())
    {
        rows.Add((
            Convert.ToInt32(reader.GetDecimal(0)),
            Convert.ToInt32(reader.GetDecimal(1)),
            Convert.ToInt32(reader.GetDecimal(2)),
            reader.GetDateTime(3)));
    }

    Console.WriteLine($"Found {rows.Count} confirmed PT bookings missing schedules.");

    foreach (var row in rows)
    {
        var memberScheduleId = (int)await Scalar("SELECT NVL(MAX(SCHEDULE_ID),0)+1 FROM MEMBER_SCHEDULE");
        var coachScheduleId = (int)await Scalar("SELECT NVL(MAX(SCHEDULE_ID),0)+1 FROM COACH_SCHEDULE");
        var start = row.Session;
        var end = start.AddHours(1);
        var date = start.Date;

        await Exec($"""
            INSERT INTO MEMBER_SCHEDULE
              (SCHEDULE_ID, MEMBER_ID, SCHEDULE_DATE, SCHEDULE_START, SCHEDULE_END, SCHEDULE_TYPE, SOURCE_RECORD_ID, STATUS)
            VALUES
              ({memberScheduleId}, {row.MemberId},
               TO_DATE('{date:yyyy-MM-dd}', 'YYYY-MM-DD'),
               TO_DATE('{start:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS'),
               TO_DATE('{end:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS'),
               'P', {row.Id}, '0')
            """);

        await Exec($"""
            INSERT INTO COACH_SCHEDULE
              (SCHEDULE_ID, COACH_ID, SCHEDULE_DATE, SCHEDULE_START, SCHEDULE_END, SCHEDULE_TYPE, SOURCE_RECORD_ID, STATUS)
            VALUES
              ({coachScheduleId}, {row.CoachId},
               TO_DATE('{date:yyyy-MM-dd}', 'YYYY-MM-DD'),
               TO_DATE('{start:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS'),
               TO_DATE('{end:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS'),
               'P', {row.Id}, N'正常')
            """);

        Console.WriteLine($"Synced PT #{row.Id} -> member schedule {memberScheduleId}, coach schedule {coachScheduleId}");
    }
}

await using (var commit = conn.CreateCommand())
{
    commit.CommandText = "COMMIT";
    await commit.ExecuteNonQueryAsync();
}

Console.WriteLine("Backfill done.");
