namespace ArdaNova.Application.Tests.ValueObjects;

using ArdaNova.Domain.ValueObjects;
using FluentAssertions;

public class UsdMoneyTests
{
    [Theory]
    [InlineData("0", 0L, "0.00")]
    [InlineData("12.34", 1234L, "12.34")]
    [InlineData("1000.10", 100010L, "1000.10")]
    public void TryParseInvariant_ProducesExactMinorUnits(string value, long expectedMinorUnits, string expectedText)
    {
        var parsed = UsdMoney.TryParseInvariant(value, out var amount);

        parsed.Should().BeTrue();
        amount.MinorUnits.Should().Be(expectedMinorUnits);
        amount.ToString().Should().Be(expectedText);
    }

    [Theory]
    [InlineData("12.345")]
    [InlineData("-0.01")]
    [InlineData("1,000.00")]
    [InlineData("not-money")]
    public void TryParseInvariant_RejectsAmbiguousOrInvalidValues(string value)
    {
        var parsed = UsdMoney.TryParseInvariant(value, out _);

        parsed.Should().BeFalse();
    }

    [Fact]
    public void TryFromDecimal_RejectsValuesOutsideTheMinorUnitRange()
    {
        var parsed = UsdMoney.TryFromDecimal(decimal.MaxValue, out _);

        parsed.Should().BeFalse();
    }

    [Fact]
    public void TryFromMinorUnits_RejectsNegativeCents()
    {
        var parsed = UsdMoney.TryFromMinorUnits(-1, out _);

        parsed.Should().BeFalse();
    }
}
