namespace ArdaNova.Application.Tests.ValueObjects;

using ArdaNova.Domain.ValueObjects;
using FluentAssertions;
using System.Globalization;

public class FixedScaleAmountTests
{
    [Theory]
    [InlineData("12.34", 2, "1234")]
    [InlineData("12.3400", 2, "1234")]
    [InlineData("0.000000000000000001", 18, "1")]
    [InlineData("10000000000000000000", 18, "10000000000000000000000000000000000000")]
    public void TryFromPositiveDecimal_UsesCanonicalBaseUnits(string text, int scale, string expectedBaseUnits)
    {
        var parsed = FixedScaleAmount.TryFromPositiveDecimal(
            decimal.Parse(text, CultureInfo.InvariantCulture), scale, out var amount);

        parsed.Should().BeTrue();
        amount.BaseUnits.Should().Be(expectedBaseUnits);
        amount.Scale.Should().Be(scale);
    }

    [Theory]
    [InlineData("0", 2)]
    [InlineData("-1", 2)]
    [InlineData("12.345", 2)]
    [InlineData("1", -1)]
    [InlineData("1", 19)]
    public void TryFromPositiveDecimal_RejectsUnrepresentableOrInvalidValues(string text, int scale)
    {
        var parsed = FixedScaleAmount.TryFromPositiveDecimal(
            decimal.Parse(text, CultureInfo.InvariantCulture), scale, out _);

        parsed.Should().BeFalse();
    }

    [Theory]
    [InlineData("1", 0)]
    [InlineData("123456789012345678901234567890", 18)]
    public void TryFromPositiveBaseUnits_AcceptsCanonicalPositiveAtoms(string atoms, int scale)
    {
        var parsed = FixedScaleAmount.TryFromPositiveBaseUnits(atoms, scale, out var amount);

        parsed.Should().BeTrue();
        amount.BaseUnits.Should().Be(atoms);
        amount.Scale.Should().Be(scale);
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("01", 0)]
    [InlineData("+1", 0)]
    [InlineData("-1", 0)]
    [InlineData("1.0", 0)]
    [InlineData("1", 19)]
    public void TryFromPositiveBaseUnits_RejectsNonCanonicalOrUnsupportedValues(string atoms, int scale)
    {
        FixedScaleAmount.TryFromPositiveBaseUnits(atoms, scale, out _).Should().BeFalse();
    }
}
