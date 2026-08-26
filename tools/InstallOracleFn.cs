#:package Oracle.ManagedDataAccess.Core@23.26.200

using System.Text.Json;
using Oracle.ManagedDataAccess.Client;

var root = Directory.GetCurrentDirectory();
if (!File.Exists(Path.Combine(root, "backend", "Api", "appsettings.Local.json")))
{
    root = Path.GetFullPath(Path.Combine(root, ".."));
}

var jsonPath = Path.Combine(root, "backend", "Api", "appsettings.Local.json");
var json = await File.ReadAllTextAsync(jsonPath);
using var doc = JsonDocument.Parse(json);
var cs = doc.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()
    ?? throw new InvalidOperationException("missing connection string");

var sqlPath = Path.Combine(root, "database", "oracle", "functions", "fn_calc_order_payable.sql");
var sql = (await File.ReadAllTextAsync(sqlPath)).Trim().TrimEnd('/').Trim();

await using var conn = new OracleConnection(cs);
await conn.OpenAsync();
await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = sql;
    await cmd.ExecuteNonQueryAsync();
}

await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT fn_calc_order_payable(1) FROM DUAL";
    var result = await cmd.ExecuteScalarAsync();
    Console.WriteLine($"fn_calc_order_payable(1) => {result}");
}

Console.WriteLine("Oracle function installed OK");
