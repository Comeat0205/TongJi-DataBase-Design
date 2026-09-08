#:package Oracle.ManagedDataAccess.Core@23.26.0
using System.Text.Json;
using Oracle.ManagedDataAccess.Client;

var root = Directory.GetCurrentDirectory();
if (!File.Exists(Path.Combine(root, "backend", "Api", "appsettings.Local.json")))
    root = Path.GetFullPath(Path.Combine(root, ".."));

var json = await File.ReadAllTextAsync(Path.Combine(root, "backend", "Api", "appsettings.Local.json"));
using var doc = JsonDocument.Parse(json);
var cs = doc.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()!;
Console.WriteLine("Using: " + cs.Split(';').FirstOrDefault(x => x.StartsWith("Data Source", StringComparison.OrdinalIgnoreCase)));

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

var memberId = (int)await Scalar("SELECT MIN(MEMBER_ID) FROM MEMBER");
if (memberId <= 0) throw new Exception("MEMBER 表为空，无法播种。");

var priceCnt = await Scalar("SELECT COUNT(*) FROM PRICE_LIST");
if (priceCnt == 0)
{
    var priceId = (int)await Scalar("SELECT NVL(MAX(PRICE_ID),0)+1 FROM PRICE_LIST");
    await Exec($@"
INSERT INTO PRICE_LIST (PRICE_ID, PRODUCT_TYPE, STANDARD_PRICE, PRICE_UPDATE_TIME)
VALUES ({priceId}, 'MEMBER_CARD_RENEW', 299, SYSDATE)");
    Console.WriteLine($"Inserted PRICE_LIST {priceId}");
}

var priceIdUsed = (int)await Scalar("SELECT MIN(PRICE_ID) FROM PRICE_LIST");
var nextV = (int)await Scalar("SELECT NVL(MAX(VOUCHER_ID),0)+1 FROM VOUCHER");
var nextO = (int)await Scalar("SELECT NVL(MAX(ORDER_ID),0)+1 FROM PAYMENT_ORDER");
var nextD = (int)await Scalar("SELECT NVL(MAX(DETAIL_ID),0)+1 FROM PAYMENT_DETAIL");
var nextBiz = (int)await Scalar("SELECT NVL(MAX(BUSINESS_ORDER_ID),90000)+1 FROM PAYMENT_ORDER");

// 可用券 + 一张刚过期（应显示过期作废）
await Exec($@"INSERT INTO VOUCHER (VOUCHER_ID, MEMBER_ID, VOUCHER_TYPE, DISCOUNT_VALUE, VALID_UNTIL, STATUS)
VALUES ({nextV}, {memberId}, N'生日福利券', 50, ADD_MONTHS(SYSDATE,1), '0')");
await Exec($@"INSERT INTO VOUCHER (VOUCHER_ID, MEMBER_ID, VOUCHER_TYPE, DISCOUNT_VALUE, VALID_UNTIL, STATUS)
VALUES ({nextV + 1}, {memberId}, N'续费折扣券', 30, ADD_MONTHS(SYSDATE,2), '0')");
await Exec($@"INSERT INTO VOUCHER (VOUCHER_ID, MEMBER_ID, VOUCHER_TYPE, DISCOUNT_VALUE, VALID_UNTIL, STATUS)
VALUES ({nextV + 2}, {memberId}, N'过期测试券', 20, TRUNC(SYSDATE)-1, '0')");

// 待支付订单（绑生日券）
await Exec($@"INSERT INTO PAYMENT_ORDER (ORDER_ID, BUSINESS_ORDER_ID, TOTAL_AMOUNT, PAYMENT_STATUS, CREATE_TIME, PAYMENT_FINISH_TIME, VOUCHER_ID)
VALUES ({nextO}, {nextBiz}, 199, N'待支付', SYSDATE, NULL, {nextV})");
await Exec($@"INSERT INTO PAYMENT_DETAIL (DETAIL_ID, ORDER_ID, PRICE_ID, TRANSACTION_PRICE, QUANTITY, SUBTOTAL_AMOUNT)
VALUES ({nextD}, {nextO}, {priceIdUsed}, 199, 1, 199)");

// 已支付订单（绑续费券并核销）
await Exec($@"UPDATE VOUCHER SET STATUS='1' WHERE VOUCHER_ID={nextV + 1}");
await Exec($@"INSERT INTO PAYMENT_ORDER (ORDER_ID, BUSINESS_ORDER_ID, TOTAL_AMOUNT, PAYMENT_STATUS, CREATE_TIME, PAYMENT_FINISH_TIME, VOUCHER_ID)
VALUES ({nextO + 1}, {nextBiz + 1}, 299, N'已支付', SYSDATE, SYSDATE, {nextV + 1})");
await Exec($@"INSERT INTO PAYMENT_DETAIL (DETAIL_ID, ORDER_ID, PRICE_ID, TRANSACTION_PRICE, QUANTITY, SUBTOTAL_AMOUNT)
VALUES ({nextD + 1}, {nextO + 1}, {priceIdUsed}, 299, 1, 299)");

await using (var commit = conn.CreateCommand())
{
    commit.CommandText = "COMMIT";
    await commit.ExecuteNonQueryAsync();
}

var vCount = await Scalar($"SELECT COUNT(*) FROM VOUCHER WHERE MEMBER_ID={memberId}");
var oCount = await Scalar("SELECT COUNT(*) FROM PAYMENT_ORDER");
Console.WriteLine($"OK member={memberId} vouchers={vCount} orders={oCount}");
Console.WriteLine($"Created vouchers {nextV}..{nextV + 2}, orders {nextO}..{nextO + 1}");
