namespace DongNamTrip.Contracts;

// Catalog: lịch khởi hành, quản trị tour (UC16, SCR16).
public record SlotDetail(int Id, int ActivityId, string ActivityName, int DestinationId, DateOnly Date, TimeOnly Start, TimeOnly End, string Status, int DurationMinutes);
public record DestinationHours(TimeOnly? Opens, TimeOnly? Closes, int VisitMinutes);
public record DepartureInfo(int Id, int TourId, string TourName, DateTime StartsAt, decimal Price, int Capacity, int Held, int Remaining, string Status, string? CancelPolicy);
public record AdminTour(int Id, string Name, string Region, string Category, string Duration, decimal ReferencePrice, string Status, int Stops, int OpenDepartures);
public record AdminTourDetail(AdminTour Tour, string Summary, string? CancelPolicy, IReadOnlyList<TourStop> Stops, IReadOnlyList<AdminDeparture> Departures);
public record TourStop(int DestinationId, string DestinationName, int Day, int Order);
public record AdminDeparture(int Id, int TourId, DateTime StartsAt, decimal Price, int Capacity, int Held, string Status);
public record TourForm(string Name, string Region, string Category, string Duration, decimal ReferencePrice, string Summary, string? CancelPolicy);
public record TourStopForm(int DestinationId, int Day, int Order);
public record DepartureForm(DateTime StartsAt, decimal Price, int Capacity);
public record StatusRequest(string Status, string? Reason = null);

/// <summary>Kết quả giữ chỗ (đặt trong Contracts để Booking dùng được mà không vi phạm quy tắc liên module).</summary>
public enum SeatHold { Held, Full, Closed, NotFound }

public record HoldResult(SeatHold Status, int Remaining);
