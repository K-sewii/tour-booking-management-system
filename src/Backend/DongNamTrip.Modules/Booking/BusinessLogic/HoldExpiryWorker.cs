namespace DongNamTrip.Modules.Booking;

/// <summary>Định kỳ chuyển đơn quá hạn giữ chỗ sang Expired và trả chỗ (UC11). Chu kỳ đọc từ Booking:ExpiryIntervalMinutes (mặc định 5).</summary>
public sealed class HoldExpiryWorker(IServiceScopeFactory scopes, IConfiguration configuration, TimeProvider time, ILogger<HoldExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = configuration.GetValue("Booking:ExpiryIntervalMinutes", 5.0);
        var interval = TimeSpan.FromMinutes(minutes > 0 ? minutes : 5.0);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<BookingAdminService>().ExpireHoldsAsync(time.GetLocalNow().DateTime, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Tác vụ hết hạn giữ chỗ gặp lỗi; sẽ thử lại ở chu kỳ sau."); }
        }
    }
}
