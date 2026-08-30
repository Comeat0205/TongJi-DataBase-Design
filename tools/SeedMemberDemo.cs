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

// 优先找登录常用会员 1，否则取 MIN(MEMBER_ID)
var memberId = (int)await Scalar("SELECT MIN(MEMBER_ID) FROM MEMBER");
var birthday = new DateTime(1989, 8, 29);
var registerDate = new DateTime(2026, 5, 1);

await Exec($@"
UPDATE MEMBER
SET BIRTHDAY = DATE '1989-08-29',
    REGISTER_DATE = DATE '2026-05-01'
WHERE MEMBER_ID = {memberId}");

var priceCnt = await Scalar("SELECT COUNT(*) FROM PRICE_LIST");
if (priceCnt == 0)
{
    await Exec("INSERT INTO PRICE_LIST (PRICE_ID, PRODUCT_TYPE, STANDARD_PRICE, PRICE_UPDATE_TIME) VALUES (1, 'MEMBER_CARD_RENEW', 299, SYSDATE)");
}

var priceId = (int)await Scalar("SELECT MIN(PRICE_ID) FROM PRICE_LIST");
var nextV = (int)await Scalar("SELECT NVL(MAX(VOUCHER_ID),0)+1 FROM VOUCHER");
var nextO = (int)await Scalar("SELECT NVL(MAX(ORDER_ID),0)+1 FROM PAYMENT_ORDER");
var nextD = (int)await Scalar("SELECT NVL(MAX(DETAIL_ID),0)+1 FROM PAYMENT_DETAIL");
var nextBiz = (int)await Scalar("SELECT NVL(MAX(BUSINESS_ORDER_ID),90000)+1 FROM PAYMENT_ORDER");

var birthdayStart = new DateTime(2026, 8, 29);
var birthdayUntil = birthdayStart.AddMonths(1);
var welcomeUntil = registerDate.AddYears(1);
var discountUntil = DateTime.Now.Date.AddDays(7);

// 券：新客 / 生日 / 员工折扣
await Exec($@"INSERT INTO VOUCHER (VOUCHER_ID, MEMBER_ID, VOUCHER_TYPE, DISCOUNT_VALUE, VALID_UNTIL, STATUS)
VALUES ({nextV}, {memberId}, N'{Welcome}', 50, DATE '{welcomeUntil:yyyy-MM-dd}', '0')");
var welcomeId = nextV++;

await Exec($@"INSERT INTO VOUCHER (VOUCHER_ID, MEMBER_ID, VOUCHER_TYPE, DISCOUNT_VALUE, VALID_UNTIL, STATUS)
VALUES ({nextV}, {memberId}, N'{Birthday}', 66, DATE '{birthdayUntil:yyyy-MM-dd}', '0')");
var birthdayId = nextV++;

await Exec($@"INSERT INTO VOUCHER (VOUCHER_ID, MEMBER_ID, VOUCHER_TYPE, DISCOUNT_VALUE, VALID_UNTIL, STATUS)
VALUES ({nextV}, {memberId}, N'{Discount}', 33, DATE '{discountUntil:yyyy-MM-dd}', '0')");
var discountId = nextV++;

// 订单1：待支付 + 生日券（199 - 66 = 133）
await Exec($@"INSERT INTO PAYMENT_ORDER (ORDER_ID, BUSINESS_ORDER_ID, TOTAL_AMOUNT, PAYMENT_STATUS, CREATE_TIME, PAYMENT_FINISH_TIME, VOUCHER_ID)
VALUES ({nextO}, {nextBiz}, 199, N'待支付', SYSDATE, NULL, {birthdayId})");
await Exec($@"INSERT INTO PAYMENT_DETAIL (DETAIL_ID, ORDER_ID, PRICE_ID, TRANSACTION_PRICE, QUANTITY, SUBTOTAL_AMOUNT)
VALUES ({nextD}, {nextO}, {priceId}, 199, 1, 199)");
nextO++; nextD++; nextBiz++;

// 订单2：已支付 + 新客券（299 - 50 = 249），核销新客券
await Exec($@"UPDATE VOUCHER SET STATUS='1' WHERE VOUCHER_ID={welcomeId}");
await Exec($@"INSERT INTO PAYMENT_ORDER (ORDER_ID, BUSINESS_ORDER_ID, TOTAL_AMOUNT, PAYMENT_STATUS, CREATE_TIME, PAYMENT_FINISH_TIME, VOUCHER_ID)
VALUES ({nextO}, {nextBiz}, 299, N'已支付', SYSDATE - 1, SYSDATE - 1, {welcomeId})");
await Exec($@"INSERT INTO PAYMENT_DETAIL (DETAIL_ID, ORDER_ID, PRICE_ID, TRANSACTION_PRICE, QUANTITY, SUBTOTAL_AMOUNT)
VALUES ({nextD}, {nextO}, {priceId}, 299, 1, 299)");
nextO++; nextD++; nextBiz++;

// 订单3：已取消 + 折扣券（149 - 33 = 116），券仍可用
await Exec($@"INSERT INTO PAYMENT_ORDER (ORDER_ID, BUSINESS_ORDER_ID, TOTAL_AMOUNT, PAYMENT_STATUS, CREATE_TIME, PAYMENT_FINISH_TIME, VOUCHER_ID)
VALUES ({nextO}, {nextBiz}, 149, N'已取消', SYSDATE - 2, NULL, {discountId})");
await Exec($@"INSERT INTO PAYMENT_DETAIL (DETAIL_ID, ORDER_ID, PRICE_ID, TRANSACTION_PRICE, QUANTITY, SUBTOTAL_AMOUNT)
VALUES ({nextD}, {nextO}, {priceId}, 149, 1, 149)");

await Exec("COMMIT");

Console.WriteLine($"OK memberId={memberId} birthday=1989-08-29 register=2026-05-01");
Console.WriteLine($"Vouchers: welcome={welcomeId}(50,至{welcomeUntil:yyyy-MM-dd},已核销) birthday={birthdayId}(66,至{birthdayUntil:yyyy-MM-dd}) discount={discountId}(33,至{discountUntil:yyyy-MM-dd})");
Console.WriteLine($"Orders: {nextO - 3}..{nextO} (待支付/已支付/已取消)");
Console.WriteLine($"Counts: vouchers={await Scalar($"SELECT COUNT(*) FROM VOUCHER WHERE MEMBER_ID={memberId}")} orders={await Scalar($"SELECT COUNT(*) FROM PAYMENT_ORDER WHERE VOUCHER_ID IN ({welcomeId},{birthdayId},{discountId})")}");
