using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Booking;

public interface IBookingStatisticsApi
{
    Task<AdminStatistics> StatisticsAsync(DateOnly from, DateOnly to, string? status, CancellationToken ct);
}
