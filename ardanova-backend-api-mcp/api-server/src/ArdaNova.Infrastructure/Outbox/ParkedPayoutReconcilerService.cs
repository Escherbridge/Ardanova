namespace ArdaNova.Infrastructure.Outbox;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>How often a parked sale is chased, how many at a time, and how old before it is.</summary>
/// <remarks>
/// Read from configuration through <see cref="Read"/> rather than bound, so that a key set to an
/// EMPTY string resolves to the stated default instead of replacing it: a binder overwrites a C#
/// default with the blank an env var most easily produces, which is how "the default fails closed"
/// stops being true in a deployment.
/// </remarks>
public sealed class ParkedPayoutReconcilerOptions
{
    /// <summary>Configuration section these values are read from.</summary>
    public const string SectionName = "Payouts:Reconciler";

    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ShortestInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan LongestInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan DefaultMinimumAge = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan LongestMinimumAge = TimeSpan.FromHours(24);

    private const int DefaultBatchSize = 25;
    private const int LargestBatchSize = 200;

    /// <summary>
    /// Whether the timed loop runs. Blank resolves to ON, and only a literal <c>false</c> turns it
    /// off.
    /// </summary>
    /// <remarks>
    /// Deliberately the wide default, and safe to be: the loop performs one READ of a receipt the
    /// sale was already submitted under. It creates no economic effect of its own, it cannot start
    /// a sale, and the row it settles is settled on what the node says. Off would mean a person's
    /// units stay set aside until somebody notices - which is the state this exists to end.
    /// </remarks>
    public bool Enabled { get; init; } = true;

    /// <summary>How long between cycles.</summary>
    public TimeSpan Interval { get; init; } = DefaultInterval;

    /// <summary>Most rows chased in one cycle.</summary>
    public int BatchSize { get; init; } = DefaultBatchSize;

    /// <summary>How long a row must have been parked before it is chased.</summary>
    /// <remarks>
    /// A sale submitted a moment ago may still be in flight at the node; asking for its receipt
    /// then invites a NotFound that would be read as a decline and hand the units back on a row
    /// that is about to settle.
    /// </remarks>
    public TimeSpan MinimumAge { get; init; } = DefaultMinimumAge;

    /// <summary>Reads the options, resolving every missing, blank or out-of-range value in code.</summary>
    public static ParkedPayoutReconcilerOptions Read(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);

        return new ParkedPayoutReconcilerOptions
        {
            Enabled = !string.Equals(
                section["Enabled"]?.Trim(), "false", StringComparison.OrdinalIgnoreCase),
            Interval = ReadSpan(
                section["IntervalSeconds"], DefaultInterval, ShortestInterval, LongestInterval),
            BatchSize = ReadCount(section["BatchSize"], DefaultBatchSize, 1, LargestBatchSize),
            MinimumAge = ReadSpan(
                section["MinimumAgeSeconds"], DefaultMinimumAge, TimeSpan.Zero, LongestMinimumAge),
        };
    }

    /// <summary>A whole number of seconds, or the stated default when it is not one.</summary>
    private static TimeSpan ReadSpan(string? value, TimeSpan fallback, TimeSpan shortest, TimeSpan longest)
    {
        if (!int.TryParse(value?.Trim(), out var seconds))
            return fallback;

        var requested = TimeSpan.FromSeconds(seconds);
        if (requested < shortest || requested > longest)
            return fallback;

        return requested;
    }

    /// <summary>A whole count, or the stated default when it is not one.</summary>
    private static int ReadCount(string? value, int fallback, int smallest, int largest)
    {
        if (!int.TryParse(value?.Trim(), out var count))
            return fallback;

        return count < smallest || count > largest ? fallback : count;
    }
}

/// <summary>What one reconciliation cycle found and what it managed to settle.</summary>
public sealed record ParkedPayoutReconcileOutcome(
    int Examined,
    int Settled,
    int StillParked,
    bool ExchangeUnavailable);

/// <summary>
/// Chases sales the node's answer left parked, so an operator's read is a fallback rather than the
/// only way a person's units come back.
/// </summary>
/// <remarks>
/// <para>
/// A sale whose answer was ambiguous - a timeout, an unreachable node, a receipt that is not
/// terminal yet - parks in <see cref="PayoutStatus.PROCESSING"/> with the seller's units still
/// locked, because returning them on a maybe is how one holding gets sold twice. Until this loop
/// existed the only thing that ever unparked such a row was somebody calling
/// <c>POST /api/Payouts/{id}/process</c>, so a seller's units stayed set aside for as long as it
/// took an operator to notice.
/// </para>
/// <para>
/// <b>It creates no economic effect.</b> Every cycle calls the same
/// <see cref="IPayoutService.ProcessPayoutAsync"/> an operator would: a READ of the receipt under
/// the key the sale was submitted with, which can only ever observe the first submission's
/// operation. Units move only against a TERMINAL receipt, and that decision stays inside the
/// payout service.
/// </para>
/// <para>
/// <b>Known limit.</b> A row the node never resolves is re-read every cycle and, because the batch
/// is taken oldest-first, a permanently stuck row can crowd out newer ones once there are more
/// parked rows than <see cref="ParkedPayoutReconcilerOptions.BatchSize"/>. Unblocked by an attempt
/// timestamp on <see cref="PayoutRequest"/> - or a lease of the shape
/// <c>EconomicOutboxLeaseStore</c> already uses - so a chased row can be put to the back of the
/// queue. Reconciling across two instances at once is likewise only safe because the receipt read
/// is idempotent; a single-winner claim is what would make the settle-and-debit step safe too.
/// </para>
/// </remarks>
public sealed class ParkedPayoutReconcilerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ParkedPayoutReconcilerOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ParkedPayoutReconcilerService> _logger;

    public ParkedPayoutReconcilerService(
        IServiceScopeFactory scopeFactory,
        ParkedPayoutReconcilerOptions options,
        TimeProvider timeProvider,
        ILogger<ParkedPayoutReconcilerService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>Settles every parked sale that is due, in one bounded pass.</summary>
    /// <remarks>
    /// Takes its dependencies as arguments rather than reading a container, so the cycle can be
    /// exercised on its own: the loop below is only a clock around it.
    /// </remarks>
    public static async Task<ParkedPayoutReconcileOutcome> ReconcileOnceAsync(
        IRepository<PayoutRequest> payouts,
        IPayoutService payoutService,
        ParkedPayoutReconcilerOptions options,
        DateTimeOffset now,
        ILogger logger,
        CancellationToken ct)
    {
        var cutoff = now.UtcDateTime - options.MinimumAge;
        var parked = await payouts.FindAsync(
            row => row.status == PayoutStatus.PROCESSING && row.requestedAt <= cutoff, ct);

        var due = parked
            .OrderBy(row => row.requestedAt)
            .Take(options.BatchSize)
            .ToList();

        var settled = 0;

        foreach (var row in due)
        {
            ct.ThrowIfCancellationRequested();

            var outcome = await payoutService.ProcessPayoutAsync(row.id, ct);

            // Nothing is wired to the node's exchange in this deployment, so no row in this cycle
            // can be settled either. Stopping here keeps one unwired deployment from producing a
            // failure line per parked sale per cycle.
            if (!outcome.IsSuccess
                && string.Equals(outcome.Error, PayoutService.NoExchangeConfigured, StringComparison.Ordinal))
            {
                logger.LogDebug(
                    "Parked-sale reconciliation stopped: {Reason}", PayoutService.NoExchangeConfigured);

                return new ParkedPayoutReconcileOutcome(
                    Examined: due.Count,
                    Settled: settled,
                    StillParked: due.Count - settled,
                    ExchangeUnavailable: true);
            }

            if (outcome.IsSuccess
                && outcome.Value is { } reconciled
                && reconciled.Status != PayoutStatus.PROCESSING)
            {
                settled++;
                logger.LogInformation(
                    "Parked sale {PayoutRequestId} settled without an operator as {Status}.",
                    row.id, reconciled.Status);
                continue;
            }

            // A sale the node has still not decided stays parked WITH ITS UNITS LOCKED. That is
            // the whole point of parking it; the next cycle asks again.
            logger.LogDebug("Parked sale {PayoutRequestId} is still unresolved.", row.id);
        }

        return new ParkedPayoutReconcileOutcome(
            Examined: due.Count,
            Settled: settled,
            StillParked: due.Count - settled,
            ExchangeUnavailable: false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.Interval);
        do
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var payouts = scope.ServiceProvider.GetRequiredService<IRepository<PayoutRequest>>();
                var payoutService = scope.ServiceProvider.GetRequiredService<IPayoutService>();

                await ReconcileOnceAsync(
                    payouts,
                    payoutService,
                    _options,
                    _timeProvider.GetUtcNow(),
                    _logger,
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "A parked-sale reconciliation cycle failed; it will retry on the next interval.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

/// <summary>Registers the parked-sale reconciler and the options it runs on.</summary>
public static class ParkedPayoutReconcilerRegistration
{
    /// <summary>
    /// Wires the timed reconciler unless configuration explicitly switches it off.
    /// </summary>
    /// <remarks>
    /// Without this call the loop does not exist and a parked sale waits for an operator, which is
    /// the state <see cref="ParkedPayoutReconcilerService"/> was written to end - so it belongs
    /// beside the other hosted registrations in <c>AddInfrastructure</c>, not behind a flag that
    /// has to be discovered.
    /// </remarks>
    public static IServiceCollection AddParkedPayoutReconciler(
        this IServiceCollection services, IConfiguration configuration)
    {
        var options = ParkedPayoutReconcilerOptions.Read(configuration);
        services.AddSingleton(options);

        if (!options.Enabled)
            return services;

        services.AddHostedService<ParkedPayoutReconcilerService>();
        return services;
    }
}
