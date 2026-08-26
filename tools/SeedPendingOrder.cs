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

var pending = await Scalar("SELECT COUNT(*) FROM PAYMENT_ORDER WHERE PAYMENT_STATUS = N'待支付'");
if (pending > 0)
{
    Console.WriteLine("Pending order already exists.");
    return;
}

var voucherId = await Scalar("SELECT NVL(MAX(VOUCHER_ID), 0) + 1 FROM VOUCHER");
var memberId = await Scalar("SELECT MIN(MEMBER_ID) FROM MEMBER");
await Exec($@"
INSERT INTO VOUCHER (VOUCHER_ID, MEMBER_ID, VOUCHER_TYPE, DISCOUNT_VALUE, VALID_UNTIL, STATUS)
VALUES ({voucherId}, {memberId}, N'续费折扣券', 20.00, ADD_MONTHS(SYSDATE, 1), '0')");

var priceId = await Scalar("SELECT MIN(PRICE_ID) FROM PRICE_LIST");
if (priceId == 0)
{
    priceId = await Scalar("SELECT NVL(MAX(PRICE_ID), 0) + 1 FROM PRICE_LIST");
    await Exec($@"
INSERT INTO PRICE_LIST (PRICE_ID, PRODUCT_TYPE, STANDARD_PRICE, PRICE_UPDATE_TIME)
VALUES ({priceId}, 'MEMBER_CARD_RENEW', 199.00, SYSDATE)");
}

var orderId = await Scalar("SELECT NVL(MAX(ORDER_ID), 0) + 1 FROM PAYMENT_ORDER");
await Exec($@"
INSERT INTO PAYMENT_ORDER (ORDER_ID, BUSINESS_ORDER_ID, TOTAL_AMOUNT, PAYMENT_STATUS, CREATE_TIME, PAYMENT_FINISH_TIME, VOUCHER_ID)
VALUES ({orderId}, 90002, 199.00, N'待支付', SYSDATE, NULL, {voucherId})");

var detailId = await Scalar("SELECT NVL(MAX(DETAIL_ID), 0) + 1 FROM PAYMENT_DETAIL");
await Exec($@"
INSERT INTO PAYMENT_DETAIL (DETAIL_ID, ORDER_ID, PRICE_ID, TRANSACTION_PRICE, QUANTITY, SUBTOTAL_AMOUNT)
VALUES ({detailId}, {orderId}, {priceId}, 199.00, 1, 199.00)");

Console.WriteLine($"Seeded pending order {orderId} with voucher {voucherId}");
