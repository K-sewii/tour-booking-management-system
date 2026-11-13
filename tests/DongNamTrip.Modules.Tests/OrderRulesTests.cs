using DongNamTrip.Contracts;
using DongNamTrip.Modules.Booking;
using Xunit;

namespace DongNamTrip.Modules.Tests;

public class OrderRulesTests
{
    private static MoneyEntry Entry(string kind, decimal amount) => new(1, kind, amount, DateTime.UtcNow, Guid.NewGuid().ToString("N")[..8]);

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Confirmed, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Expired, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.CancelRequested, true)]
    [InlineData(OrderStatus.Expired, OrderStatus.Confirmed, true)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.CancelRequested, true)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Completed, true)]
    [InlineData(OrderStatus.CancelRequested, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.CancelRequested, OrderStatus.Pending, true)]
    [InlineData(OrderStatus.CancelRequested, OrderStatus.Confirmed, true)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending, false)]
    [InlineData(OrderStatus.Completed, OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Expired, OrderStatus.CancelRequested, false)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Pending, false)]
    [InlineData(OrderStatus.Pending, OrderStatus.Completed, false)]
    public void CanMoveFollowsStm07(string from, string to, bool expected) => Assert.Equal(expected, OrderRules.CanMove(from, to));

    [Fact]
    public void MoneyFormulasKeepReceiptsAndRefundsSeparate()
    {
        MoneyEntry[] entries = [Entry(MoneyKinds.Receipt, 600_000), Entry(MoneyKinds.Receipt, 200_000), Entry(MoneyKinds.Refund, 300_000)];
        Assert.Equal(800_000, OrderRules.Paid(entries));
        Assert.Equal(300_000, OrderRules.Refunded(entries));
        Assert.Equal(500_000, OrderRules.Refundable(entries));
        Assert.Equal(500_000, OrderRules.Outstanding(1_000_000, entries));
    }

    [Fact]
    public void OutstandingNeverNegative()
    {
        MoneyEntry[] entries = [Entry(MoneyKinds.Receipt, 1_200_000)];
        Assert.Equal(0, OrderRules.Outstanding(1_000_000, entries));
        Assert.Equal(1_000_000, OrderRules.Outstanding(1_000_000, []));
        Assert.Equal(0, OrderRules.Refundable([]));
    }

    [Theory]
    [InlineData(690_000, 0.3, 207_000)]
    [InlineData(1_000_001, 0.3, 300_000)]
    [InlineData(1_000_000, 2.0, 1_000_000)]
    [InlineData(1_000_000, 0.0, 0)]
    public void DepositIsRoundedAndNeverAbovePrice(decimal price, decimal rate, decimal expected) => Assert.Equal(expected, OrderRules.Deposit(price, rate));
}
