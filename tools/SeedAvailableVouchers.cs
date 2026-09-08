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

var memberId = await Scalar("SELECT MIN(MEMBER_ID) FROM MEMBER");
var next = await Scalar("SELECT NVL(MAX(VOUCHER_ID), 0) + 1 FROM VOUCHER");

// 两张可用券：同额时测「马上过期优先」；再来一张更高额测「优惠最多优先」
await Exec($@"
INSERT INTO VOUCHER (VOUCHER_ID, MEMBER_ID, VOUCHER_TYPE, DISCOUNT_VALUE, VALID_UNTIL, STATUS)
VALUES ({next}, {memberId}, N'续费折扣券A', 25.00, SYSDATE + 2, '0')");
await Exec($@"
INSERT INTO VOUCHER (VOUCHER_ID, MEMBER_ID, VOUCHER_TYPE, DISCOUNT_VALUE, VALID_UNTIL, STATUS)
VALUES ({next + 1}, {memberId}, N'续费折扣券B', 25.00, SYSDATE + 10, '0')");
await Exec($@"
INSERT INTO VOUCHER (VOUCHER_ID, MEMBER_ID, VOUCHER_TYPE, DISCOUNT_VALUE, VALID_UNTIL, STATUS)
VALUES ({next + 2}, {memberId}, N'生日福利券', 40.00, SYSDATE + 20, '0')");
await Exec($@"
INSERT INTO VOUCHER (VOUCHER_ID, MEMBER_ID, VOUCHER_TYPE, DISCOUNT_VALUE, VALID_UNTIL, STATUS)
VALUES ({next + 3}, {memberId}, N'过期测试券', 15.00, SYSDATE - 1, '0')");

Console.WriteLine($"Seeded vouchers {next}..{next + 3} for member {memberId}");
Console.WriteLine("Auto-select should prefer 生日福利券(40). If only A/B available, prefer A (sooner expiry).");
