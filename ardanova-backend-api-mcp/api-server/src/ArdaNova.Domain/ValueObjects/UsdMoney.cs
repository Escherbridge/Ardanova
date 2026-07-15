namespace ArdaNova.Domain.ValueObjects;

using System.Globalization;

/// <summary>Represents a non-negative USD amount in exact cents.</summary>
public readonly record struct UsdMoney
{
    public const int MinorUnitsPerMajorUnit = 100;

    private UsdMoney(long minorUnits) => MinorUnits = minorUnits;

    /// <summary>Gets the exact amount in USD cents.</summary>
    public long MinorUnits { get; }

    /// <summary>Gets whether the amount is greater than zero.</summary>
    public bool IsPositive => MinorUnits > 0;

    /// <summary>Creates an amount from exact, non-negative USD cents.</summary>
    public static bool TryFromMinorUnits(long minorUnits, out UsdMoney amount)
    {
        if (minorUnits < 0)
        {
            amount = default;
            return false;
        }

        amount = new UsdMoney(minorUnits);
        return true;
    }

    /// <summary>Creates an amount from a non-negative USD value with at most two decimal places.</summary>
    public static bool TryFromDecimal(decimal value, out UsdMoney amount)
    {
        if (value < 0)
        {
            amount = default;
            return false;
        }

        try
        {
            var scaled = value * MinorUnitsPerMajorUnit;
            if (scaled != decimal.Truncate(scaled) || scaled > long.MaxValue)
            {
                amount = default;
                return false;
            }

            amount = new UsdMoney((long)scaled);
            return true;
        }
        catch (OverflowException)
        {
            amount = default;
            return false;
        }
    }

    /// <summary>Parses a non-negative, invariant-culture USD amount with at most two decimal places.</summary>
    public static bool TryParseInvariant(string? value, out UsdMoney amount)
    {
        amount = default;
        return decimal.TryParse(
                   value,
                   NumberStyles.AllowDecimalPoint,
                   CultureInfo.InvariantCulture,
                   out var parsed)
               && TryFromDecimal(parsed, out amount);
    }

    /// <summary>Returns the exact major-unit decimal amount.</summary>
    public decimal ToDecimal() => MinorUnits / (decimal)MinorUnitsPerMajorUnit;

    /// <inheritdoc />
    public override string ToString() => ToDecimal().ToString("F2", CultureInfo.InvariantCulture);
}
