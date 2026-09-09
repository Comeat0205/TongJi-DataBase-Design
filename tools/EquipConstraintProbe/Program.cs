using System.Text.Json;
using Oracle.ManagedDataAccess.Client;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var localSettings = Path.Combine(root, "backend", "Api", "appsettings.Local.json");
if (!File.Exists(localSettings))
{
    throw new FileNotFoundException("未找到 appsettings.Local.json", localSettings);
}

using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(localSettings));
var cs = doc.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()
    ?? throw new InvalidOperationException("缺少 DefaultConnection");

var triggerSql = await File.ReadAllTextAsync(
    Path.Combine(root, "database", "oracle", "triggers", "trg_repair_update_equipment.sql"));

// 只执行 CREATE OR REPLACE ... END 块（去掉尾部查询）
var endMarker = "END TRG_REPAIR_UPDATE_EQUIPMENT;";
var endIndex = triggerSql.IndexOf(endMarker, StringComparison.OrdinalIgnoreCase);
if (endIndex < 0)
{
    throw new InvalidOperationException("触发器脚本格式不正确。");
}

var ddl = triggerSql[..(endIndex + endMarker.Length)];

await using var conn = new OracleConnection(cs);
await conn.OpenAsync();
await using var cmd = conn.CreateCommand();
cmd.CommandText = ddl;
await cmd.ExecuteNonQueryAsync();

cmd.CommandText = """
    SELECT STATUS
    FROM USER_TRIGGERS
    WHERE TRIGGER_NAME = 'TRG_REPAIR_UPDATE_EQUIPMENT'
    """;
var status = Convert.ToString(await cmd.ExecuteScalarAsync());
Console.WriteLine($"TRG_REPAIR_UPDATE_EQUIPMENT => {status}");

cmd.CommandText = """
    SELECT COUNT(*)
    FROM USER_ERRORS
    WHERE NAME = 'TRG_REPAIR_UPDATE_EQUIPMENT'
    """;
var errors = Convert.ToInt32(await cmd.ExecuteScalarAsync());
Console.WriteLine($"compile_errors => {errors}");
if (errors > 0)
{
    Environment.ExitCode = 1;
}
