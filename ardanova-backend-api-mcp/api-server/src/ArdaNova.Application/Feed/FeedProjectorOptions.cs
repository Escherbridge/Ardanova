namespace ArdaNova.Application.Feed;

using System.Globalization;
using Microsoft.Extensions.Configuration;

/// <summary>Operator settings for the feed projector; see Feed/AGENTS.md §Configuration.</summary>
public sealed class FeedProjectorOptions
{
    /// <summary>Configuration section this binds from (env: Feed__Projector__Enabled, ...).</summary>
    public const string SectionName = "Feed:Projector";

    /// <summary>Whether the timed projector runs. Blank resolves to ON in Development and OFF everywhere else.</summary>
    public bool Enabled { get; init; }

    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>How far back each cycle re-reads source rows, so a slow commit is still seen.</summary>
    public TimeSpan LookBack { get; init; } = TimeSpan.FromMinutes(10);

    public TimeSpan SweepInterval { get; init; } = TimeSpan.FromHours(1);

    /// <summary>How far back the sweep re-checks envelopes against their sources.</summary>
    public TimeSpan SweepWindow { get; init; } = TimeSpan.FromHours(48);

    /// <summary>Reads the operator's settings; a malformed or out-of-range value is a startup failure.</summary>
    public static FeedProjectorOptions FromConfiguration(IConfiguration configuration, string? environmentName)
    {
        var defaults = new FeedProjectorOptions();
        var isDevelopment = string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);

        var interval = TimeSpan.FromSeconds(ReadInteger(
            configuration, "IntervalSeconds", (int)defaults.Interval.TotalSeconds, 1, 300));
        var lookBack = TimeSpan.FromMinutes(ReadInteger(
            configuration, "LookBackMinutes", (int)defaults.LookBack.TotalMinutes, 1, 1440));
        if (lookBack < interval * 2)
            throw new InvalidOperationException(
                $"{SectionName}:LookBackMinutes must cover at least two cycles of {SectionName}:IntervalSeconds, "
                + "or a row committed between two cycles can be missed.");

        return new FeedProjectorOptions
        {
            Enabled = ReadBoolean(configuration, "Enabled", isDevelopment),
            Interval = interval,
            LookBack = lookBack,
            SweepInterval = TimeSpan.FromMinutes(ReadInteger(
                configuration, "SweepIntervalMinutes", (int)defaults.SweepInterval.TotalMinutes, 5, 1440)),
            SweepWindow = TimeSpan.FromHours(ReadInteger(
                configuration, "SweepWindowHours", (int)defaults.SweepWindow.TotalHours, 1, 720)),
        };
    }

    private static bool ReadBoolean(IConfiguration configuration, string key, bool fallback)
    {
        var raw = configuration[$"{SectionName}:{key}"];
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        return bool.TryParse(raw.Trim(), out var parsed)
            ? parsed
            : throw new InvalidOperationException($"{SectionName}:{key} must be true or false; '{raw}' is neither.");
    }

    private static int ReadInteger(IConfiguration configuration, string key, int fallback, int minimum, int maximum)
    {
        var raw = configuration[$"{SectionName}:{key}"];
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        if (!int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            || parsed < minimum
            || parsed > maximum)
        {
            throw new InvalidOperationException(
                $"{SectionName}:{key} must be a whole number between {minimum} and {maximum}; got '{raw}'.");
        }

        return parsed;
    }
}
