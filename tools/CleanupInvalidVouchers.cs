#:package Oracle.ManagedDataAccess.Core@23.26.0
using System.Text.Json;
using Oracle.ManagedDataAccess.Client;

var root = Directory.GetCurrentDirectory();
if (!File.Exists(Path.Combine(root, "backend", "Api", "appsettings.Local.json")))
    root = Path.GetFullPath(Path.Combine(root, ".."));

var json = await File.ReadAllTextAsync(Path.Combine(root, "backend", "Api", "appsettings.Local.json"));
using var doc = JsonDocument.Parse(json);
var cs = doc.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()!;

const string Birthday = "生日福利券";
const string Welcome = "新客体验券";
const string Discount = "折扣券";

await using var conn = new OracleConnection(cs);
await conn.OpenAsync();

async Task<int> ScalarInt(string sql)
{
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    return Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
}

async Task Exec(string sql)
{
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    await cmd.ExecuteNonQueryAsync();
}

Console.WriteLine("==== BEFORE ====");
var totalBefore = await ScalarInt("SELECT COUNT(*) FROM VOUCHER");
Console.WriteLine($"total vouchers={totalBefore}");

await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = @"
SELECT VOUCHER_TYPE, DISCOUNT_VALUE, COUNT(*)
FROM VOUCHER
GROUP BY VOUCHER_TYPE, DISCOUNT_VALUE
ORDER BY VOUCHER_TYPE, DISCOUNT_VALUE";
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
    {
        Console.WriteLine($"  type={r.GetString(0)} amount={r.GetDecimal(1)} count={r.GetInt32(2)}");
    }
}

// 保留：三种固定类型 + 对应面额；其余全部视为不符合要求的测试数据。
var invalidCount = await ScalarInt($@"
SELECT COUNT(*) FROM VOUCHER
WHERE NOT (
  (VOUCHER_TYPE = '{Birthday}' AND DISCOUNT_VALUE = 66)
  OR (VOUCHER_TYPE = '{Welcome}' AND DISCOUNT_VALUE = 50)
  OR (VOUCHER_TYPE = '{Discount}' AND DISCOUNT_VALUE = 33)
)");

Console.WriteLine($"invalid vouchers to remove={invalidCount}");

if (invalidCount == 0)
{
    Console.WriteLine("Nothing to delete.");
    return;
}

// 订单解绑将被删除的券，避免外键冲突。
await Exec($@"
UPDATE PAYMENT_ORDER
SET VOUCHER_ID = NULL
WHERE VOUCHER_ID IN (
  SELECT VOUCHER_ID FROM VOUCHER
  WHERE NOT (
    (VOUCHER_TYPE = '{Birthday}' AND DISCOUNT_VALUE = 66)
    OR (VOUCHER_TYPE = '{Welcome}' AND DISCOUNT_VALUE = 50)
    OR (VOUCHER_TYPE = '{Discount}' AND DISCOUNT_VALUE = 33)
  )
)");

await Exec($@"
DELETE FROM VOUCHER
WHERE NOT (
  (VOUCHER_TYPE = '{Birthday}' AND DISCOUNT_VALUE = 66)
  OR (VOUCHER_TYPE = '{Welcome}' AND DISCOUNT_VALUE = 50)
  OR (VOUCHER_TYPE = '{Discount}' AND DISCOUNT_VALUE = 33)
)");

await Exec("COMMIT");

Console.WriteLine("==== AFTER ====");
var totalAfter = await ScalarInt("SELECT COUNT(*) FROM VOUCHER");
Console.WriteLine($"total vouchers={totalAfter}");
await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = @"
SELECT VOUCHER_TYPE, DISCOUNT_VALUE, COUNT(*)
FROM VOUCHER
GROUP BY VOUCHER_TYPE, DISCOUNT_VALUE
ORDER BY VOUCHER_TYPE, DISCOUNT_VALUE";
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
    {
        Console.WriteLine($"  type={r.GetString(0)} amount={r.GetDecimal(1)} count={r.GetInt32(2)}");
    }
}

Console.WriteLine("CLEANUP_OK");
