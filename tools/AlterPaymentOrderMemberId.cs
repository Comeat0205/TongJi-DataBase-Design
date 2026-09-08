#:package Oracle.ManagedDataAccess.Core@23.26.200

using System.Text.Json;
using Oracle.ManagedDataAccess.Client;

var root = Directory.GetCurrentDirectory();
if (!File.Exists(Path.Combine(root, "backend", "Api", "appsettings.Local.json")))
    root = Path.GetFullPath(Path.Combine(root, ".."));

var json = await File.ReadAllTextAsync(Path.Combine(root, "backend", "Api", "appsettings.Local.json"));
using var doc = JsonDocument.Parse(json);
var cs = doc.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()!;

await using var conn = new OracleConnection(cs);
await conn.OpenAsync();

async Task Exec(string sql)
{
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    await cmd.ExecuteNonQueryAsync();
}

try
{
    await Exec("ALTER TABLE PAYMENT_ORDER ADD MEMBER_ID NUMBER(10)");
    Console.WriteLine("Added MEMBER_ID column.");
}
catch (OracleException ex) when (ex.Number == 1430)
{
    Console.WriteLine("MEMBER_ID already exists.");
}

await Exec(@"
UPDATE PAYMENT_ORDER o
SET MEMBER_ID = (
  SELECT v.MEMBER_ID FROM VOUCHER v WHERE v.VOUCHER_ID = o.VOUCHER_ID
)
WHERE o.MEMBER_ID IS NULL AND o.VOUCHER_ID IS NOT NULL");

Console.WriteLine("Backfilled MEMBER_ID from vouchers.");
