using Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.Services;

/// <summary>
/// 每 10 分钟记录主训练馆实时在场人数与占用率到 CAPACITYLOG（容量波形图数据源）。
/// </summary>
public sealed class CapacitySnapshotBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CapacitySnapshotBackgroundService> _logger;

    public CapacitySnapshotBackgroundService(
        IServiceProvider services,
        ILogger<CapacitySnapshotBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CapacitySnapshotBackgroundService 已启动：主训练馆每 10 分钟采样一次。");

        // 启动后立即补采当前 10 分钟档，便于联调立刻看到点。
        await SnapshotOnceAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var next = AlignToTenMinutes(now).AddMinutes(10);
            var delay = next - now;
            if (delay < TimeSpan.FromSeconds(1))
            {
                delay = TimeSpan.FromSeconds(1);
            }

            _logger.LogInformation(
                "下次主训练馆容量采样将在 {NextRun}（约 {Delay} 后）。",
                next.ToString("yyyy-MM-dd HH:mm:ss"),
                delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await SnapshotOnceAsync(stoppingToken);
        }
    }

    private async Task SnapshotOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _services.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ICheckInOutAppService>();
            var created = await svc.RecordMainVenueSnapshotAsync(ct);
            if (created is null)
            {
                _logger.LogDebug("本档已有采样或未找到主训练馆，跳过写入。");
                return;
            }

            _logger.LogInformation(
                "已记录主训练馆容量：{Time} 在场={Count} 占用率={Rate}%",
                created.LogTimestamp?.ToString("HH:mm"),
                created.RecordedCount,
                created.OccupancyRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "主训练馆容量采样失败：{Message}", ex.Message);
        }
    }

    private static DateTime AlignToTenMinutes(DateTime dt) =>
        new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute / 10 * 10, 0);
}
