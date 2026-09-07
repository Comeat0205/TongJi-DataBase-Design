using Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.Services;

/// <summary>
/// 每天 00:05 为今日生日会员发放生日福利券（依据 MEMBER.BIRTHDAY）。
/// 会员打开「我的优惠券」时也会懒加载补发，本服务用于无人访问时的定时发放。
/// </summary>
public sealed class BirthdayVoucherBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BirthdayVoucherBackgroundService> _logger;

    public BirthdayVoucherBackgroundService(
        IServiceProvider services,
        ILogger<BirthdayVoucherBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BirthdayVoucherBackgroundService 已启动，将于每天 00:05 发放生日福利券。");

        // 启动后先跑一次，覆盖当天已过 00:05 的情况。
        await IssueTodayAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = now.Date.AddDays(1).AddHours(0).AddMinutes(5);
            var delay = nextRun - now;
            _logger.LogInformation(
                "距离下次生日券发放还有 {Delay}，将在 {NextRun} 执行。",
                delay,
                nextRun.ToString("yyyy-MM-dd HH:mm:ss"));

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await IssueTodayAsync(stoppingToken);
        }
    }

    private async Task IssueTodayAsync(CancellationToken ct)
    {
        _logger.LogInformation("开始为今日生日会员发放生日福利券...");

        try
        {
            using var scope = _services.CreateScope();
            var payment = scope.ServiceProvider.GetRequiredService<IPaymentAppService>();
            var count = await payment.IssueBirthdayVouchersForTodayAsync(ct);
            _logger.LogInformation("生日福利券发放完成，本次新发 {Count} 张。", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生日福利券发放失败：{Message}", ex.Message);
        }
    }
}
