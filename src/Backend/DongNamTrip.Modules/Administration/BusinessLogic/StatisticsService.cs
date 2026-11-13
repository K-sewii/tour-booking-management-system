using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Administration;

/// <summary>Thống kê vận hành (UC24, SCR13): ba chỉ tiêu tách riêng, không có dữ liệu thì trả 0 và danh sách rỗng.</summary>
public sealed class StatisticsService(StatisticsRepository repository)
{
    public async Task<OpResult<OperationSummary>> SummaryAsync(DateTime from, DateTime to, string? status, CancellationToken ct)
    {
        if (!BusinessDate.InRange(from) || !BusinessDate.InRange(to) || to < from) return Op.Invalid("Khoảng ngày không hợp lệ: ngày kết thúc phải sau ngày bắt đầu.");
        if (!string.IsNullOrWhiteSpace(status) && !OrderStatus.All.Contains(status)) return Op.Invalid("Trạng thái đơn không hợp lệ.");
        // Ngày nhập theo giờ Việt Nam, không kèm giờ là cả ngày; dữ liệu lưu theo UTC.
        var start = BusinessDate.ToUtc(from);
        var end = BusinessDate.EndExclusiveUtc(to);
        var (received, refunded) = await repository.MoneyTotalsAsync(start, end, ct);
        return Op.Ok(new OperationSummary(from, to, await repository.OrdersByStatusAsync(start, end, status, ct), await repository.OrderValueAsync(start, end, status, ct),
            received, refunded, await repository.RequestsByStatusAsync(start, end, ct)));
    }
}
