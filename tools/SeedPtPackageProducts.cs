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

var coachId = (int)await Scalar("SELECT NVL(MIN(COACH_ID), 0) FROM COACH");
if (coachId <= 0)
{
    throw new Exception("COACH 表为空，无法播种私教课程。");
}

var courses = new (int Id, string Name, string Desc)[]
{
    (9101, "力量塑形私教", "针对深蹲、硬拉等复合动作的一对一力量课，适合增肌塑形。"),
    (9102, "减脂燃脂私教", "有氧结合力量的减脂私教方案，控制配速与心率区间。"),
    (9103, "体态康复私教", "肩颈腰体态评估与矫正训练，强度较低，适合康复期。"),
};

foreach (var course in courses)
{
    var exists = await Scalar($"SELECT COUNT(*) FROM PERSONAL_COURSE WHERE PERSONAL_COURSE_ID={course.Id}");
    if (exists > 0)
    {
        Console.WriteLine($"PERSONAL_COURSE {course.Id} already exists");
        continue;
    }

    await Exec($@"
INSERT INTO PERSONAL_COURSE (PERSONAL_COURSE_ID, COURSE_NAME, COURSE_DESCRIPTION, COACH_ID)
VALUES ({course.Id}, N'{course.Name}', N'{course.Desc}', {coachId})");
    Console.WriteLine($"Inserted PERSONAL_COURSE {course.Id}");
}

var products = new (string Type, decimal Price)[]
{
    ("PT_PACKAGE_9101_12_365", 2880m),
    ("PT_PACKAGE_9102_10_180", 2380m),
    ("PT_PACKAGE_9103_8_120", 1980m),
};

foreach (var product in products)
{
    var exists = await Scalar($"SELECT COUNT(*) FROM PRICE_LIST WHERE PRODUCT_TYPE='{product.Type}'");
    if (exists > 0)
    {
        Console.WriteLine($"PRICE_LIST {product.Type} already exists");
        continue;
    }

    var priceId = (int)await Scalar("SELECT NVL(MAX(PRICE_ID),0)+1 FROM PRICE_LIST");
    await Exec($@"
INSERT INTO PRICE_LIST (PRICE_ID, PRODUCT_TYPE, STANDARD_PRICE, PRICE_UPDATE_TIME)
VALUES ({priceId}, '{product.Type}', {product.Price}, TRUNC(SYSDATE))");
    Console.WriteLine($"Inserted PRICE_LIST {priceId} {product.Type}");
}

await using (var commit = conn.CreateCommand())
{
    commit.CommandText = "COMMIT";
    await commit.ExecuteNonQueryAsync();
}

var productCount = await Scalar("SELECT COUNT(*) FROM PRICE_LIST WHERE PRODUCT_TYPE LIKE 'PT_PACKAGE_%'");
Console.WriteLine($"OK PT_PACKAGE products={productCount}");
