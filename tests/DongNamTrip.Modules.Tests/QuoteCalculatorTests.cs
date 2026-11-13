using DongNamTrip.Contracts;
using DongNamTrip.Modules.Quotation;
using Xunit;

namespace DongNamTrip.Modules.Tests;

public class QuoteCalculatorTests
{
    private static readonly DateTime Now = new(2030, 1, 1, 8, 0, 0);

    private static QuoteItemForm Item(int quantity, decimal price, bool inPackage = false) => new("Dịch vụ", "gói", quantity, price, inPackage);

    private static QuoteForm Form(IReadOnlyList<QuoteItemForm>? items = null, decimal deposit = 100_000, string policy = "Hoàn 80%", DateTime? until = null) =>
        new(items ?? [Item(2, 500_000)], deposit, policy, until ?? Now.AddDays(3));

    [Fact]
    public void TotalAddsOnlyItemsOutsideThePackage()
    {
        Assert.Equal(1_300_000, QuoteCalculator.Total([Item(2, 500_000), Item(1, 300_000), Item(4, 100_000, inPackage: true)]));
        Assert.Equal(0, QuoteCalculator.Total([Item(3, 100_000, inPackage: true)]));
        Assert.Equal(0, QuoteCalculator.Total([]));
    }

    [Fact]
    public void ValidFormHasNoErrors() => Assert.Empty(QuoteCalculator.Validate(Form(), Now));

    [Fact]
    public void RequiresItemsAndPositiveQuantityAndNonNegativePrice()
    {
        Assert.Contains(nameof(QuoteForm.Items), QuoteCalculator.Validate(Form(items: []), Now).Keys);
        Assert.Contains(nameof(QuoteForm.Items), QuoteCalculator.Validate(Form(items: [Item(0, 1000)]), Now).Keys);
        Assert.Contains(nameof(QuoteForm.Items), QuoteCalculator.Validate(Form(items: [Item(1, -1)]), Now).Keys);
    }

    [Fact]
    public void ValidUntilMustBeInTheFuture()
    {
        Assert.Contains(nameof(QuoteForm.ValidUntil), QuoteCalculator.Validate(Form(until: Now), Now).Keys);
        Assert.Contains(nameof(QuoteForm.ValidUntil), QuoteCalculator.Validate(Form(until: Now.AddMinutes(-1)), Now).Keys);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1_000_000, false)]
    [InlineData(1_000_001, true)]
    public void DepositStaysBetweenZeroAndTotal(double deposit, bool invalid) =>
        Assert.Equal(invalid, QuoteCalculator.Validate(Form(items: [Item(2, 500_000)], deposit: (decimal)deposit), Now).ContainsKey(nameof(QuoteForm.Deposit)));

    [Fact]
    public void CancelPolicyIsRequired() => Assert.Contains(nameof(QuoteForm.CancelPolicy), QuoteCalculator.Validate(Form(policy: " "), Now).Keys);
}
