namespace ArdaNova.Domain.ValueObjects;

using System.Globalization;
using System.Numerics;

/// <summary>Represents a positive asset amount as canonical base units at a fixed scale.</summary>
public readonly record struct FixedScaleAmount
{
    private const int MaximumScale = 18;

    private FixedScaleAmount(string baseUnits, int scale)
    {
        BaseUnits = baseUnits;
        Scale = scale;
    }

    /// <summary>Gets the unsigned, canonical integer atom count.</summary>
    public string BaseUnits { get; }

    /// <summary>Gets the number of decimal places implied by the asset.</summary>
    public int Scale { get; }

    /// <summary>Determines whether a scale is representable by commerce amounts.</summary>
    public static bool IsSupportedScale(int scale)
        => scale is >= 0 and <= MaximumScale;

    /// <summary>Converts a positive decimal without rounding or culture-dependent formatting.</summary>
    public static bool TryFromPositiveDecimal(decimal value, int scale, out FixedScaleAmount amount)
    {
        amount = default;
        if (value <= 0 || !IsSupportedScale(scale))
            return false;

        var bits = decimal.GetBits(value);
        var sourceScale = (bits[3] >> 16) & 0x7F;
        var mantissa = new BigInteger((uint)bits[0])
            | (new BigInteger((uint)bits[1]) << 32)
            | (new BigInteger((uint)bits[2]) << 64);
        var scaleDelta = scale - sourceScale;

        if (scaleDelta < 0)
        {
            var divisor = BigInteger.Pow(10, -scaleDelta);
            if (mantissa % divisor != 0)
                return false;

            mantissa /= divisor;
        }
        else if (scaleDelta > 0)
        {
            mantissa *= BigInteger.Pow(10, scaleDelta);
        }

        if (mantissa <= 0)
            return false;

        amount = new FixedScaleAmount(mantissa.ToString(CultureInfo.InvariantCulture), scale);
        return true;
    }
}
