#:package Oracle.ManagedDataAccess.Core@23.26.0
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

var memberIds = new List<int>();
await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT MEMBER_ID FROM MEMBER ORDER BY MEMBER_ID FETCH FIRST 5 ROWS ONLY";
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync()) memberIds.Add(Convert.ToInt32(r.GetDecimal(0)));
}
if (memberIds.Count == 0) throw new Exception("MEMBER 为空");

var priceId = (int)await Scalar("SELECT MIN(PRICE_ID) FROM PRICE_LIST");
if (priceId <= 0)
{
    priceId = 1;
    await Exec($"INSERT INTO PRICE_LIST (PRICE_ID, PRODUCT_TYPE, STANDARD_PRICE, PRICE_UPDATE_TIME) VALUES (1, 'MEMBER_CARD_RENEW', 299, SYSDATE)");
}

var nextV = (int)await Scalar("SELECT NVL(MAX(VOUCHER_ID),0)+1 FROM VOUCHER");
var nextO = (int)await Scalar("SELECT NVL(MAX(ORDER_ID),0)+1 FROM PAYMENT_ORDER");
var nextD = (int)await Scalar("SELECT NVL(MAX(DETAIL_ID),0)+1 FROM PAYMENT_DETAIL");
var nextBiz = (int)await Scalar("SELECT NVL(MAX(BUSINESS_ORDER_ID),90000)+1 FROM PAYMENT_ORDER");

var types = new[] { "生日福利券", "续费折扣券", "新客体验券", "节日满减券", "私教体验券", "团课折扣券" };
var amounts = new[] { 99m, 149m, 199m, 299m, 399m, 499m };
var statuses = new[] { "待支付", "已支付", "已取消" };

var vCreated = 0;
var oCreated = 0;

foreach (var mid in memberIds)
{
    // 每人 4 张可用券 + 1 张过期
    for (var i = 0; i < 4; i++)
    {
        var t = types[(nextV + i) % types.Length];
        var disc = 10 + i * 10 + mid;
        var days = 5 + i * 7;
        await Exec($@"INSERT INTO VOUCHER (VOUCHER_ID, MEMBER_ID, VOUCHER_TYPE, DISCOUNT_VALUE, VALID_UNTIL, STATUS)
VALUES ({nextV}, {mid}, N'{t}', {disc}, SYSDATE + {days}, '0')");
        nextV++;
        vCreated++;
    }
    await Exec($@"INSERT INTO VOUCHER (VOUCHER_ID, MEMBER_ID, VOUCHER_TYPE, DISCOUNT_VALUE, VALID_UNTIL, STATUS)
VALUES ({nextV}, {mid}, N'过期测试券', 15, TRUNC(SYSDATE)-1, '0')");
    nextV++;
    vCreated++;

    // 每人 3 笔订单：待支付 / 已支付 / 已取消
    for (var i = 0; i < 3; i++)
    {
        var amt = amounts[(nextO + i) % amounts.Length];
        var st = statuses[i];
        int? voucherId = null;

        if (st == "待支付")
        {
            // 绑一张该会员未使用券
            voucherId = (int)await Scalar($@"
SELECT MIN(VOUCHER_ID) FROM VOUCHER
WHERE MEMBER_ID={mid} AND STATUS='0' AND TRUNC(VALID_UNTIL)>=TRUNC(SYSDATE)
  AND VOUCHER_ID NOT IN (SELECT VOUCHER_ID FROM PAYMENT_ORDER WHERE VOUCHER_ID IS NOT NULL)");
        }
        else if (st == "已支付")
        {
            voucherId = (int)await Scalar($@"
SELECT MIN(VOUCHER_ID) FROM VOUCHER
WHERE MEMBER_ID={mid} AND STATUS='0' AND TRUNC(VALID_UNTIL)>=TRUNC(SYSDATE)
  AND VOUCHER_ID NOT IN (SELECT VOUCHER_ID FROM PAYMENT_ORDER WHERE VOUCHER_ID IS NOT NULL)");
            if (voucherId > 0)
                await Exec($"UPDATE VOUCHER SET STATUS='1' WHERE VOUCHER_ID={voucherId}");
            else
                voucherId = null;
        }

        var finish = st == "已支付" ? "SYSDATE" : "NULL";
        var vSql = voucherId is > 0 ? voucherId.ToString() : "NULL";
        await Exec($@"INSERT INTO PAYMENT_ORDER (ORDER_ID, BUSINESS_ORDER_ID, TOTAL_AMOUNT, PAYMENT_STATUS, CREATE_TIME, PAYMENT_FINISH_TIME, VOUCHER_ID)
VALUES ({nextO}, {nextBiz}, {amt}, N'{st}', SYSDATE - {i}, {finish}, {vSql})");
        await Exec($@"INSERT INTO PAYMENT_DETAIL (DETAIL_ID, ORDER_ID, PRICE_ID, TRANSACTION_PRICE, QUANTITY, SUBTOTAL_AMOUNT)
VALUES ({nextD}, {nextO}, {priceId}, {amt}, 1, {amt})");
        nextO++;
        nextD++;
        nextBiz++;
        oCreated++;
    }
}

await using (var commit = conn.CreateCommand())
{
    commit.CommandText = "COMMIT";
    await commit.ExecuteNonQueryAsync();
}

Console.WriteLine($"Seeded members={string.Join(',', memberIds)} vouchers+={vCreated} orders+={oCreated}");
Console.WriteLine($"Totals: vouchers={await Scalar("SELECT COUNT(*) FROM VOUCHER")} orders={await Scalar("SELECT COUNT(*) FROM PAYMENT_ORDER")}");
