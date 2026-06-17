using Domain.Shared.Models;

namespace Domain.UnitTests.Shared.Models;

public class ValueObjectTests
{
    private sealed class Money : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    private sealed class Address : ValueObject
    {
        public string? Street { get; }
        public string? City { get; }

        public Address(string? street, string? city)
        {
            Street = street;
            City = city;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
        }
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        var money1 = new Money(100.00m, "USD");
        var money2 = new Money(100.00m, "USD");

        Assert.True(money1.Equals(money2));
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        var money1 = new Money(100.00m, "USD");
        var money2 = new Money(200.00m, "USD");

        Assert.False(money1.Equals(money2));
    }

    [Fact]
    public void Equals_WithDifferentCurrency_ReturnsFalse()
    {
        var money1 = new Money(100.00m, "USD");
        var money2 = new Money(100.00m, "EUR");

        Assert.False(money1.Equals(money2));
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        var money = new Money(100.00m, "USD");

        Assert.False(money.Equals(null));
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        var money = new Money(100.00m, "USD");
        var address = new Address("123 Main St", "NYC");

        Assert.False(money.Equals(address));
    }

    [Fact]
    public void GetHashCode_WithSameValues_ReturnsSameHash()
    {
        var money1 = new Money(100.00m, "USD");
        var money2 = new Money(100.00m, "USD");

        Assert.Equal(money1.GetHashCode(), money2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ReturnsDifferentHash()
    {
        var money1 = new Money(100.00m, "USD");
        var money2 = new Money(200.00m, "USD");

        Assert.NotEqual(money1.GetHashCode(), money2.GetHashCode());
    }

    [Fact]
    public void EqualityOperator_WithSameValues_ReturnsTrue()
    {
        var money1 = new Money(100.00m, "USD");
        var money2 = new Money(100.00m, "USD");

        Assert.True(money1 == money2);
    }

    [Fact]
    public void EqualityOperator_WithDifferentValues_ReturnsFalse()
    {
        var money1 = new Money(100.00m, "USD");
        var money2 = new Money(200.00m, "USD");

        Assert.False(money1 == money2);
    }

    [Fact]
    public void InequalityOperator_WithDifferentValues_ReturnsTrue()
    {
        var money1 = new Money(100.00m, "USD");
        var money2 = new Money(200.00m, "USD");

        Assert.True(money1 != money2);
    }

    [Fact]
    public void EqualityOperator_WithBothNull_ReturnsTrue()
    {
        Money? money1 = null;
        Money? money2 = null;

        Assert.True(money1 == money2);
    }

    [Fact]
    public void EqualityOperator_WithOneNull_ReturnsFalse()
    {
        var money1 = new Money(100.00m, "USD");
        Money? money2 = null;

        Assert.False(money1 == money2);
        Assert.False(money2 == money1);
    }

    [Fact]
    public void Equals_WithNullableComponents_WorksCorrectly()
    {
        var address1 = new Address(null, "NYC");
        var address2 = new Address(null, "NYC");
        var address3 = new Address("123 Main St", "NYC");

        Assert.True(address1.Equals(address2));
        Assert.False(address1.Equals(address3));
    }
}
