using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.Services;

/// <summary>
/// 每天 23:00 自动调用 sp_auto_checkout 存储过程，
/// 将所有未签退的入场记录签退并重置场馆容量（功能点 #21）。
/// </summary>
public sealed class AutoCheckoutBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AutoCheckoutBackgroundService> _logger;

    public AutoCheckoutBackgroundService(
        IServiceProvider services,
        ILogger<AutoCheckoutBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutoCheckoutBackgroundService 已启动，等待每天 23:00 执行自动签退。");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = now.Hour >= 23
                ? now.Date.AddDays(1).AddHours(23)   // 今天已过 23 点，等明天
                : now.Date.AddHours(23);              // 今天还没到 23 点

            var delay = nextRun - now;
            _logger.LogInformation("距离下次自动签退还有 {Delay}，将在 {NextRun} 执行。",
                delay, nextRun.ToString("yyyy-MM-dd HH:mm:ss"));

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ExecuteAutoCheckoutAsync(stoppingToken);
        }
    }

    private async Task ExecuteAutoCheckoutAsync(CancellationToken ct)
    {
        _logger.LogInformation("开始执行 23:00 自动签退（sp_auto_checkout）...");

        try
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 调用 Oracle 存储过程 sp_auto_checkout
            await context.Database.ExecuteSqlRawAsync(
                "BEGIN sp_auto_checkout; END;", ct);

            _logger.LogInformation("23:00 自动签退执行成功。");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "23:00 自动签退执行失败：{Message}", ex.Message);
        }
    }
}
