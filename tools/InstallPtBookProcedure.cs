#:package Oracle.ManagedDataAccess.Core@23.26.0
using System.Text.Json;
using System.Text.RegularExpressions;
using Oracle.ManagedDataAccess.Client;

var root = Directory.GetCurrentDirectory();
if (!File.Exists(Path.Combine(root, "backend", "Api", "appsettings.Local.json")))
    root = Path.GetFullPath(Path.Combine(root, ".."));

var json = await File.ReadAllTextAsync(Path.Combine(root, "backend", "Api", "appsettings.Local.json"));
using var doc = JsonDocument.Parse(json);
var cs = doc.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()!;

var sqlPath = Path.Combine(root, "database", "oracle", "procedures", "sp_book_personal_training.sql");
var raw = await File.ReadAllTextAsync(sqlPath);
var match = Regex.Match(
    raw,
    @"CREATE OR REPLACE PROCEDURE[\s\S]*?END;\s*/",
    RegexOptions.IgnoreCase);
if (!match.Success)
    throw new Exception("未在 SQL 文件中找到 CREATE OR REPLACE PROCEDURE。");

var sql = match.Value.Trim().TrimEnd('/').Trim();

await using var conn = new OracleConnection(cs);
await conn.OpenAsync();
await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = sql;
    await cmd.ExecuteNonQueryAsync();
}

await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = """
        SELECT STATUS FROM USER_OBJECTS
        WHERE OBJECT_TYPE = 'PROCEDURE' AND OBJECT_NAME = 'SP_BOOK_PERSONAL_TRAINING'
        """;
    var status = await cmd.ExecuteScalarAsync();
    Console.WriteLine($"SP_BOOK_PERSONAL_TRAINING status={status}");
}

Console.WriteLine("Oracle procedure installed OK");
