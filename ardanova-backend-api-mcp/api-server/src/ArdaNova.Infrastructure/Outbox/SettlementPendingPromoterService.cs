namespace ArdaNova.Infrastructure.Outbox;

using System.Globalization;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Finishes sponsorships the node confirmed after the request that started them had gone.
/// </summary>
/// <remarks>
/// <para>
/// A <c>SETTLEMENT_PENDING</c> intent used to be promoted only when somebody READ it: the sponsor
/// had to still have the success page open, or come back to it, for their own stake to be
/// recorded. A person who closed the tab was left with a sponsorship that the node had completed
/// and ArdaNova had not. This loop is the thing that reads on their behalf.
/// </para>
/// <para>
/// It contains no promotion logic of its own on purpose. Every promotion goes through
/// <see cref="IFundingIntentService.GetStatusAsync"/>, acting as the intent's OWN funder, so the
/// ownership check on that path still runs and there is exactly one code path that can turn a
/// pending sponsorship into a stake, a treasury receipt and a movement on the funding gate. A
/// second copy here is how the two would drift.
/// </para>
/// <para>
/// It never dispatches. <c>GetStatusAsync</c> reconciles - a READ of an operation that already
/// exists - which is why a poll may make it and a dispatch may not, and why running this loop can
/// never create a second economic effect.
/// </para>
/// </remarks>
public sealed class SettlementPendingPromoter
{
    private readonly IRepository<FundingIntent> _intentRepository;
    private readonly IFundingIntentService _fundingIntents;
    private readonly SettlementPendingPromoterOptions _options;
    private readonly ILogger<SettlementPendingPromoter> _logger;

    public SettlementPendingPromoter(
        IRepository<FundingIntent> intentRepository,
        IFundingIntentService fundingIntents,
        SettlementPendingPromoterOptions options,
        ILogger<SettlementPendingPromoter> logger)
    {
        _intentRepository = intentRepository;
        _fundingIntents = fundingIntents;
        _options = options;
        _logger = logger;
    }

    /// <summary>Advances one bounded batch of pending sponsorships, oldest first.</summary>
    public async Task<SettlementPendingPromotionRun> PromoteOnceAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return SettlementPendingPromotionRun.Disabled;

        var now = DateTime.UtcNow;

        // The same grace period the outbox waits, for the same reason: below it the request that
        // created the sponsorship may still be finishing it, and two loops on one settlement is a
        // race rather than a safety net.
        var cutoff = now - FundingIntentService.OutboxHandoverDelay;
        var staleBefore = now - _options.StaleAfter;

        var pending = await _intentRepository.FindAsync(
            intent => intent.status == FundingIntentStatus.SETTLEMENT_PENDING
                && intent.updatedAt <= cutoff,
            ct);

        if (pending.Count == 0)
            return SettlementPendingPromotionRun.Empty;

        // A backlog larger than one batch is itself the alarm: the loop is keeping up, or it is
        // not, and an operator can only see which if the number is said out loud.
        if (pending.Count > _options.BatchSize)
        {
            _logger.LogWarning(
                "{PendingCount} sponsorships are waiting on the node and only {BatchSize} are advanced per cycle.",
                pending.Count,
                _options.BatchSize);
        }

        var batch = pending
            .OrderBy(intent => intent.updatedAt)
            .Take(_options.BatchSize)
            .ToList();

        var promoted = 0;
        var failed = 0;
        foreach (var intent in batch)
        {
            if (ct.IsCancellationRequested)
                break;

            if (intent.updatedAt <= staleBefore)
            {
                _logger.LogWarning(
                    "Sponsorship {IntentId} has been waiting on the node since {PendingSince}; it needs an operator.",
                    intent.id,
                    intent.updatedAt.ToString("O", CultureInfo.InvariantCulture));
            }

            try
            {
                // The row's own funder is the actor. The ownership check on GetStatusAsync still
                // runs; it simply cannot refuse an identity taken from the row it is guarding.
                var status = await _fundingIntents.GetStatusAsync(intent.id, intent.funderUserId, ct);
                if (!status.IsSuccess)
                {
                    failed++;
                    _logger.LogWarning(
                        "Sponsorship {IntentId} could not be advanced: {Reason}",
                        intent.id,
                        status.Error);
                    continue;
                }

                if (!string.Equals(
                        status.Value?.Status,
                        nameof(FundingIntentStatus.SETTLEMENT_PENDING),
                        StringComparison.Ordinal))
                {
                    promoted++;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception failure)
            {
                // One sponsorship that throws must not strand the rest of the batch.
                failed++;
                _logger.LogError(
                    failure,
                    "Advancing sponsorship {IntentId} threw; the next cycle will try again.",
                    intent.id);
            }
        }

        return new SettlementPendingPromotionRun(
            Enabled: true,
            Examined: batch.Count,
            Promoted: promoted,
            Failed: failed);
    }
}

/// <summary>What one promotion cycle did, so a caller and a log can say the same thing.</summary>
/// <param name="Examined">Pending sponsorships this cycle looked at.</param>
/// <param name="Promoted">Sponsorships that left <c>SETTLEMENT_PENDING</c> during the cycle.</param>
/// <param name="Failed">Sponsorships the cycle could not advance; the next cycle retries them.</param>
public sealed record SettlementPendingPromotionRun(bool Enabled, int Examined, int Promoted, int Failed)
{
    public static SettlementPendingPromotionRun Disabled { get; } = new(false, 0, 0, 0);

    public static SettlementPendingPromotionRun Empty { get; } = new(true, 0, 0, 0);
}

/// <summary>Operator limits for the pending-sponsorship promoter.</summary>
/// <remarks>
/// <b>Blank resolves to ON, deliberately.</b> The L0 rule that an option deciding WHO MAY DO
/// SOMETHING must resolve blank to the narrowest setting does not apply here, and applying it
/// would recreate the very defect this class closes: an unset or empty variable would silently
/// switch off the loop that finishes people's sponsorships. This option grants no authority - the
/// promoter acts as each row's own funder and only ever reconciles, which is a read - so the safe
/// resolution of "the operator said nothing" is that the safety net runs. Only a value that parses
/// as <c>false</c> turns it off, and an out-of-range number is a startup failure rather than a
/// silent zero.
/// </remarks>
public sealed class SettlementPendingPromoterOptions
{
    /// <summary>Configuration section this binds from.</summary>
    public const string SectionName = "Azoa:SettlementPendingPromoter";

    public bool Enabled { get; init; } = true;

    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(60);

    public int BatchSize { get; init; } = 25;

    /// <summary>How long a sponsorship may wait on the node before each cycle names it in the log.</summary>
    public TimeSpan StaleAfter { get; init; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Reads the operator's settings, resolving every absent or blank value to the documented
    /// default IN CODE rather than leaving it to a binder. <c>Get&lt;T&gt;()</c> replaces a C#
    /// default with the empty value an environment variable set to "" produces, so each key is
    /// parsed by hand here instead.
    /// </summary>
    public static SettlementPendingPromoterOptions FromConfiguration(IConfiguration configuration)
    {
        var defaults = new SettlementPendingPromoterOptions();

        var enabled = ReadBoolean(configuration, "Enabled", defaults.Enabled);
        var intervalSeconds = ReadInteger(
            configuration, "IntervalSeconds", (int)defaults.Interval.TotalSeconds, 5, 900);
        var batchSize = ReadInteger(configuration, "BatchSize", defaults.BatchSize, 1, 200);
        var staleAfterHours = ReadInteger(
            configuration, "StaleAfterHours", (int)defaults.StaleAfter.TotalHours, 1, 720);

        return new SettlementPendingPromoterOptions
        {
            Enabled = enabled,
            Interval = TimeSpan.FromSeconds(intervalSeconds),
            BatchSize = batchSize,
            StaleAfter = TimeSpan.FromHours(staleAfterHours),
        };
    }

    private static bool ReadBoolean(IConfiguration configuration, string key, bool fallback)
    {
        var raw = configuration[$"{SectionName}:{key}"];
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        return bool.TryParse(raw.Trim(), out var parsed)
            ? parsed
            : throw new InvalidOperationException(
                $"{SectionName}:{key} must be true or false; '{raw}' is neither.");
    }

    private static int ReadInteger(
        IConfiguration configuration,
        string key,
        int fallback,
        int minimum,
        int maximum)
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

/// <summary>Runs the promoter on a timer, one bounded cycle at a time, in its own scope.</summary>
public sealed class SettlementPendingPromoterHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SettlementPendingPromoterOptions _options;
    private readonly ILogger<SettlementPendingPromoterHostedService> _logger;

    public SettlementPendingPromoterHostedService(
        IServiceScopeFactory scopeFactory,
        SettlementPendingPromoterOptions options,
        ILogger<SettlementPendingPromoterHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.Interval);
        do
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var promoter = scope.ServiceProvider.GetRequiredService<SettlementPendingPromoter>();
                var run = await promoter.PromoteOnceAsync(stoppingToken);
                if (run.Promoted > 0 || run.Failed > 0)
                {
                    _logger.LogInformation(
                        "Advanced {Examined} pending sponsorships: {Promoted} finished, {Failed} still waiting.",
                        run.Examined,
                        run.Promoted,
                        run.Failed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception failure)
            {
                _logger.LogError(
                    failure,
                    "The pending-sponsorship promotion cycle failed; it will retry on the next interval.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

/// <summary>Registration for the promoter, so one call site turns the safety net on.</summary>
public static class SettlementPendingPromoterRegistration
{
    /// <summary>
    /// Registers the pending-sponsorship promoter and its hosted timer. Without this call the
    /// promoter is inert and a sponsorship is once again only finished when somebody reads it.
    /// </summary>
    public static IServiceCollection AddSettlementPendingPromoter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = SettlementPendingPromoterOptions.FromConfiguration(configuration);
        services.AddSingleton(options);
        services.AddScoped<SettlementPendingPromoter>();
        if (options.Enabled)
            services.AddHostedService<SettlementPendingPromoterHostedService>();

        return services;
    }
}
