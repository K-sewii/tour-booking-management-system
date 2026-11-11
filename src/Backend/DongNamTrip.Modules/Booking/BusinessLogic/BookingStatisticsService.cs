using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Booking;

public sealed class BookingStatisticsService(BookingRepository repository) : IBookingStatisticsApi
{
    public Task<AdminStatistics> StatisticsAsync(DateOnly from, DateOnly to, string? status, CancellationToken ct) => repository.StatisticsAsync(from, to, status, ct);
}
