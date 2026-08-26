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

if (await Scalar("SELECT COUNT(*) FROM PAYMENT_ORDER") > 0)
{
    Console.WriteLine("PAYMENT_ORDER already has data.");
    return;
}

if (await Scalar("SELECT COUNT(*) FROM PRICE_LIST") == 0)
{
    var priceId = await Scalar("SELECT NVL(MAX(PRICE_ID), 0) + 1 FROM PRICE_LIST");
    await Exec($@"
INSERT INTO PRICE_LIST (PRICE_ID, PRODUCT_TYPE, STANDARD_PRICE, PRICE_UPDATE_TIME)
VALUES ({priceId}, 'MEMBER_CARD_RENEW', 299.00, SYSDATE)");
}

var priceIdUsed = await Scalar("SELECT MIN(PRICE_ID) FROM PRICE_LIST");
var voucherId = await Scalar("SELECT MIN(VOUCHER_ID) FROM VOUCHER");
var orderId = await Scalar("SELECT NVL(MAX(ORDER_ID), 0) + 1 FROM PAYMENT_ORDER");

await Exec($@"
INSERT INTO PAYMENT_ORDER (ORDER_ID, BUSINESS_ORDER_ID, TOTAL_AMOUNT, PAYMENT_STATUS, CREATE_TIME, PAYMENT_FINISH_TIME, VOUCHER_ID)
VALUES ({orderId}, 90001, 299.00, N'已支付', SYSDATE, SYSDATE, {voucherId})");

var detailId = await Scalar("SELECT NVL(MAX(DETAIL_ID), 0) + 1 FROM PAYMENT_DETAIL");
await Exec($@"
INSERT INTO PAYMENT_DETAIL (DETAIL_ID, ORDER_ID, PRICE_ID, TRANSACTION_PRICE, QUANTITY, SUBTOTAL_AMOUNT)
VALUES ({detailId}, {orderId}, {priceIdUsed}, 299.00, 1, 299.00)");

Console.WriteLine($"Inserted order {orderId}, detail {detailId}, voucher {voucherId}");
Console.WriteLine($"payable={await Scalar($"SELECT fn_calc_order_payable({orderId}) FROM DUAL")}");
