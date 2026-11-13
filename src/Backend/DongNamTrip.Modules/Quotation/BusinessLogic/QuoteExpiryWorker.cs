namespace DongNamTrip.Modules.Quotation;

/// <summary>Định kỳ chuyển báo giá quá hạn sang Expired (UC10). Chu kỳ đọc từ Quotation:ExpiryIntervalMinutes (mặc định 15).</summary>
public sealed class QuoteExpiryWorker(IServiceScopeFactory scopes, IConfiguration configuration, TimeProvider time, ILogger<QuoteExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = configuration.GetValue("Quotation:ExpiryIntervalMinutes", 15.0);
        var interval = TimeSpan.FromMinutes(minutes > 0 ? minutes : 15.0);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<QuotationAdminService>().ExpireQuotesAsync(time.GetLocalNow().DateTime, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Tác vụ hết hạn báo giá gặp lỗi; sẽ thử lại ở chu kỳ sau."); }
        }
    }
}
