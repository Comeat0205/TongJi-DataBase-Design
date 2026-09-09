using Api.Middleware;
using Api.Serialization;
using Api.Services;
using Application.Extensions;
using Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 本地敏感配置（账密等），已被 .gitignore 排除，不入库；example 见 appsettings.Local.example.json。
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// 统一在入口项目完成依赖装配，避免控制器直接关心底层实现。
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 所有 DateTime 按北京墙钟输出，不带 Z，避免前端再按 UTC 错算
        options.JsonSerializerOptions.Converters.Add(new BeijingDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new BeijingNullableDateTimeConverter());
    });
// 当前保留 OpenAPI，便于课程设计阶段联调接口。
builder.Services.AddOpenApi();

// 功能点 #21：每天 23:00 自动签退后台服务
builder.Services.AddHostedService<AutoCheckoutBackgroundService>();
// 主训练馆容量：每 10 分钟写入 CAPACITYLOG，供员工端波形图
builder.Services.AddHostedService<CapacitySnapshotBackgroundService>();
// 生日福利券：每天 00:05 按 MEMBER.BIRTHDAY 自动发放
builder.Services.AddHostedService<BirthdayVoucherBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 全局异常处理要尽量靠前，统一拦截后续请求链中的异常。
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();


