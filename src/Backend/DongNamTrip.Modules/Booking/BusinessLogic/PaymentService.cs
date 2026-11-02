using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence.Entities;

namespace DongNamTrip.Modules.Booking;

/// <summary>Quy tắc xem đơn, giao dịch và chọn phương thức thanh toán của khách. Chưa có cổng thanh toán thật.</summary>
public sealed class PaymentService(BookingRepository repository)
{
    public async Task<List<PaymentEntry>> ListAsync(int accountId, CancellationToken ct) =>
        (await repository.OrdersAsync(accountId, ct)).Select(ToEntry).ToList();

    /// <returns>Null nếu đơn không tồn tại hoặc thuộc tài khoản khác.</returns>
    public async Task<PaymentEntry?> GetAsync(int accountId, int orderId, CancellationToken ct) =>
        await repository.OrderAsync(accountId, orderId, ct) is { } order ? ToEntry(order) : null;

    public async Task<PaymentChoiceDto> ChoiceAsync(int accountId, CancellationToken ct)
    {
        var choice = await repository.PaymentChoiceAsync(accountId, ct);
        return new(choice?.MethodCode, choice?.UpdatedAt);
    }

    /// <returns>Null nếu phương thức không được hỗ trợ.</returns>
    public async Task<PaymentChoiceDto?> SaveChoiceAsync(int accountId, string? method, CancellationToken ct)
    {
        if (!PaymentMethods.IsSupported(method)) return null;
        var choice = await repository.SavePaymentChoiceAsync(accountId, method!, DateTime.UtcNow, ct);
        return new(choice.MethodCode, choice.UpdatedAt);
    }

    private static PaymentEntry ToEntry(Dondat order) => new(
        order.MaDon, order.NgayDat, order.TrangThai, order.GiaChot, order.SoKhach, order.MaBg, order.MaLkh,
        order.Giaodichtien.OrderByDescending(entry => entry.ThoiDiem)
            .Select(entry => new MoneyEntry(entry.MaGd, entry.LoaiGd, entry.SoTien, entry.ThoiDiem, entry.MaThamChieu)).ToArray());
}
