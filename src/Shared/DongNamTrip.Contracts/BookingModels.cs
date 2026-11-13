namespace DongNamTrip.Contracts;

// Booking: đặt tour, hủy, xác nhận, thu hoàn (UC11-13, UC19, UC20).
public record BookingForm(int DepartureId, int Guests, string Contact);
public record BookingInfo(int Id, string Source, int? DepartureId, int? QuoteId, string Title, DateTime PlacedAt, int Guests, decimal AgreedPrice, decimal RequiredDeposit, DateTime? HoldUntil, DateOnly? TripDate, string Status);
public record BookingDetail(BookingInfo Booking, string Contact, string CancelPolicy, string? CancelReason, decimal Paid, decimal Refunded, decimal Outstanding, decimal Refundable, IReadOnlyList<MoneyEntry> Transactions,
    IReadOnlyList<ProgramStop>? Program = null, IReadOnlyList<BookingAdjustment>? Adjustments = null);
/// <summary>Một điểm trong chương trình đã chốt (US12 AC1). Đơn tour riêng để Program rỗng; lịch trình lấy ở GET /api/account/quotes/{id}/program.</summary>
public record ProgramStop(int Day, string Place);
/// <summary>Một lần nhân viên điều chỉnh đơn sau xác nhận (US12 AC3); Price là giá chốt sau điều chỉnh nếu có đổi giá.</summary>
public record BookingAdjustment(DateTime At, string Change, string Reason, decimal? Price);
public record CancelRequest(string Reason);
public record BookingFilter(string? Status, DateTime? From, DateTime? To, string? Q, bool? Overdue, int? Page, int? Size);
public record AdminBooking(int Id, int AccountId, string Customer, string Source, string Title, DateTime PlacedAt, int Guests, decimal AgreedPrice, decimal Paid, string Status, DateTime? HoldUntil = null);
public record AdminBookingDetail(AdminBooking Booking, BookingDetail Detail, IReadOnlyList<string> MissingForConfirm);
public record AdjustForm(string Change, string Reason, string CustomerConsent, decimal? NewPrice);
public record CancelDecision(bool Approve, string Reason);
public record MoneyForm(string Kind, decimal Amount, string Reference, DateTime At);
public record QuoteAcceptance(int QuoteId, int RequestId, int AccountId, int Guests, decimal Amount, decimal Deposit, string CancelPolicy, DateOnly TripStart, string Contact);
