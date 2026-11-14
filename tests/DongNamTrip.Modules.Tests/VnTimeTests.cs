using Xunit;

namespace DongNamTrip.Modules.Tests;

public class VnTimeTests
{
    [Fact]
    public void ToUtcKeepsUtc()
    {
        var utc = new DateTime(2026, 11, 20, 5, 30, 0, DateTimeKind.Utc);
        Assert.Equal(utc, VnTime.ToUtc(utc));
    }

    [Fact]
    public void ToUtcTreatsUnspecifiedAsVietnamTime()
    {
        var result = VnTime.ToUtc(new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Unspecified));
        Assert.Equal(new DateTime(2026, 11, 19, 17, 0, 0, DateTimeKind.Utc), result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void ToUtcConvertsLocalByItsOwnOffset()
    {
        // Chuỗi có múi (+07:00) được parser đổi thành Local theo máy chủ; kết quả không được lệch theo múi giờ máy chủ.
        var local = new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Local);
        var result = VnTime.ToUtc(local);
        Assert.Equal(local.ToUniversalTime(), result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void ToUtcDoesNotOverflowAtMinValue()
    {
        var result = VnTime.ToUtc(DateTime.MinValue);
        Assert.Equal(DateTime.MinValue.Ticks, result.Ticks);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void ToUtcFromCalendarDayUsesFixedOffset()
    {
        Assert.Equal(new DateTime(2026, 11, 19, 17, 0, 0, DateTimeKind.Utc), VnTime.ToUtc(new DateOnly(2026, 11, 20), TimeOnly.MinValue));
        Assert.Equal(new DateTime(2026, 11, 27, 16, 59, 59, DateTimeKind.Utc), VnTime.ToUtc(new DateOnly(2026, 11, 27), new TimeOnly(23, 59, 59)));
    }

    [Fact]
    public void AsUtcKeepsUtcAndLabelsUnspecifiedAsUtc()
    {
        var unspecified = new DateTime(2026, 11, 19, 17, 0, 0, DateTimeKind.Unspecified);
        var result = VnTime.AsUtc(unspecified);
        Assert.Equal(unspecified.Ticks, result.Ticks);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void AsUtcConvertsLocal()
    {
        var local = new DateTime(2026, 11, 20, 8, 0, 0, DateTimeKind.Local);
        Assert.Equal(local.ToUniversalTime(), VnTime.AsUtc(local));
    }

    [Fact]
    public void FromUtcShowsTheVietnameseDay()
    {
        // Quyền nghe bắt đầu 00:00 ngày 20/11 giờ VN = 17:00 ngày 19/11 UTC; khách phải đọc "20/11", không phải "19/11".
        var start = new DateTime(2026, 11, 19, 17, 0, 0, DateTimeKind.Utc);
        Assert.Equal("20/11/2026", VnTime.FromUtc(start).ToString("dd/MM/yyyy"));
    }

    [Fact]
    public void EndExclusiveUtcIncludesTheWholeDayWhenNoTimeGiven()
    {
        Assert.Equal(new DateTime(2026, 11, 20, 17, 0, 0, DateTimeKind.Utc), VnTime.EndExclusiveUtc(new DateTime(2026, 11, 20)));
        Assert.Equal(new DateTime(2026, 11, 20, 3, 0, 0, DateTimeKind.Utc), VnTime.EndExclusiveUtc(new DateTime(2026, 11, 20, 10, 0, 0)));
    }
}
