namespace ArdaNova.Application.Feed;

using System.Globalization;
using Microsoft.Extensions.Configuration;

/// <summary>Operator settings for the feed projector; see Feed/AGENTS.md §Configuration.</summary>
public sealed class FeedProjectorOptions
{
    /// <summary>Configuration section this binds from (env: Feed__Projector__Enabled, ...).</summary>
    public const string SectionName = "Feed:Projector";

    /// <summary>Whether the timed projector runs. OFF unless the operator sets it true, in every environment.</summary>
    public bool Enabled { get; init; }

    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>How far back each cycle re-reads source rows, so a slow commit is still seen.</summary>
    public TimeSpan LookBack { get; init; } = TimeSpan.FromMinutes(10);

    public TimeSpan SweepInterval { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// How many envelopes the sweep reads, judges and deletes per round trip. The sweep always reconciles
    /// every envelope (see Feed/AGENTS.md §Sweep); this bounds each batch's query/transaction size, not how
    /// much of the table gets covered.
    /// </summary>
    public int SweepBatchSize { get; init; } = 500;

    /// <summary>Reads the operator's settings; a malformed or out-of-range value throws.</summary>
    public static FeedProjectorOptions FromConfiguration(IConfiguration configuration)
    {
        var defaults = new FeedProjectorOptions();

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
            Enabled = ReadBoolean(configuration, "Enabled", false),
            Interval = interval,
            LookBack = lookBack,
            SweepInterval = TimeSpan.FromMinutes(ReadInteger(
                configuration, "SweepIntervalMinutes", (int)defaults.SweepInterval.TotalMinutes, 5, 1440)),
            SweepBatchSize = ReadInteger(
                configuration, "SweepBatchSize", defaults.SweepBatchSize, 1, 10_000),
        };
    }

    /// <summary>
    /// Reads the settings without throwing: a malformed value yields defaults with the projector OFF and the
    /// reason in <paramref name="problem"/>, so a feed typo never stops the API (payments, sign-in) starting.
    /// </summary>
    public static bool TryFromConfiguration(
        IConfiguration configuration,
        out FeedProjectorOptions options,
        out string? problem)
    {
        try
        {
            options = FromConfiguration(configuration);
            problem = null;
            return true;
        }
        catch (InvalidOperationException malformed)
        {
            options = new FeedProjectorOptions { Enabled = false };
            problem = malformed.Message;
            return false;
        }
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
