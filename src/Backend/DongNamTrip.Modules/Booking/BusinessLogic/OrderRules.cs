using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Booking;

/// <summary>Quy tắc thuần của đơn: máy trạng thái STM-07 và công thức tiền ("đã thanh toán = tổng thu - tổng hoàn"). Không truy cập dữ liệu.</summary>
public static class OrderRules
{
    private static readonly HashSet<(string From, string To)> Moves =
    [
        (OrderStatus.Pending, OrderStatus.Confirmed),
        (OrderStatus.Pending, OrderStatus.Expired),
        (OrderStatus.Pending, OrderStatus.CancelRequested),
        (OrderStatus.Expired, OrderStatus.Confirmed),
        (OrderStatus.Confirmed, OrderStatus.CancelRequested),
        (OrderStatus.Confirmed, OrderStatus.Completed),
        (OrderStatus.CancelRequested, OrderStatus.Cancelled),
        // Từ chối hủy: trả về trạng thái trước (TrangThaiTruocHuy chỉ nhận Pending hoặc Confirmed).
        (OrderStatus.CancelRequested, OrderStatus.Pending),
        (OrderStatus.CancelRequested, OrderStatus.Confirmed)
    ];

    /// <summary>Nhãn nhật ký của lần điều chỉnh đơn và dấu ngăn lý do với ý kiến đồng ý của khách; ghi ở BookingAdminService, đọc lại ở BookingService.</summary>
    public const string AdjustAction = AuditActions.OrderAdjusted;
    public const string ConsentSeparator = " | Ý kiến khách: ";

    public static bool CanMove(string from, string to) => Moves.Contains((from, to));

    /// <summary>Tổng thu.</summary>
    public static decimal Paid(IEnumerable<MoneyEntry> entries) => entries.Where(entry => entry.Kind == MoneyKinds.Receipt).Sum(entry => entry.Amount);

    /// <summary>Tổng hoàn.</summary>
    public static decimal Refunded(IEnumerable<MoneyEntry> entries) => entries.Where(entry => entry.Kind == MoneyKinds.Refund).Sum(entry => entry.Amount);

    /// <summary>Còn phải thu = max(0, giá chốt - (thu - hoàn)).</summary>
    public static decimal Outstanding(decimal agreedPrice, IEnumerable<MoneyEntry> entries)
    {
        var list = entries as IReadOnlyCollection<MoneyEntry> ?? entries.ToList();
        return Math.Max(0, agreedPrice - (Paid(list) - Refunded(list)));
    }

    /// <summary>Số tiền tối đa có thể hoàn = thu - hoàn.</summary>
    public static decimal Refundable(IEnumerable<MoneyEntry> entries)
    {
        var list = entries as IReadOnlyCollection<MoneyEntry> ?? entries.ToList();
        return Paid(list) - Refunded(list);
    }

    /// <summary>Khoản cần thu để xác nhận (tour có sẵn: giá chốt x tỉ lệ cọc, làm tròn 0 chữ số; không vượt giá chốt).</summary>
    public static decimal Deposit(decimal agreedPrice, decimal rate) => Math.Min(agreedPrice, Math.Round(agreedPrice * Math.Clamp(rate, 0m, 1m), 0, MidpointRounding.AwayFromZero));
}
