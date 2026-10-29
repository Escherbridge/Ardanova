namespace ArdaNova.Infrastructure.Azoa;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>Reads <c>Azoa:Exchange</c> into a validated <see cref="ExchangeAdmissionPolicy"/>; a bad combination fails boot.</summary>
/// <remarks>Each key parsed by hand, blank meaning the documented default (the promoter's pattern). See Azoa/AGENTS.md §Settlement gateway.</remarks>
public static class AzoaExchangeAdmissionSettings
{
    public const string AdmitWindowSecondsKey = "AdmitWindowSeconds";
    public const string QuietWindowSecondsKey = "QuietWindowSeconds";
    public const string MaximumClockSkewSecondsKey = "MaximumClockSkewSeconds";
    public const string NodeEnforcesAdmitBeforeKey = "NodeEnforcesAdmitBefore";

    /// <summary>The startup check: parses, then throws unless W &gt; W_admit + skew.</summary>
    public static ExchangeAdmissionPolicy FromConfiguration(IConfiguration configuration)
    {
        var defaults = ExchangeAdmissionPolicy.Default;
        var policy = new ExchangeAdmissionPolicy
        {
            AdmitWindow = ReadSeconds(configuration, AdmitWindowSecondsKey, defaults.AdmitWindow),
            QuietWindow = ReadSeconds(configuration, QuietWindowSecondsKey, defaults.QuietWindow),
            MaximumClockSkew = ReadSeconds(configuration, MaximumClockSkewSecondsKey, defaults.MaximumClockSkew),
            NodeEnforcesAdmitBefore = ReadBoolean(configuration, NodeEnforcesAdmitBeforeKey, defaults.NodeEnforcesAdmitBefore),
        };
        policy.Validate();
        return policy;
    }

    private static TimeSpan ReadSeconds(IConfiguration configuration, string key, TimeSpan fallback)
    {
        var raw = configuration[$"{ExchangeAdmissionPolicy.SectionName}:{key}"];
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        return int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds)
            ? TimeSpan.FromSeconds(seconds)
            : throw new InvalidOperationException(
                $"{ExchangeAdmissionPolicy.SectionName}:{key} must be a whole number of seconds; got '{raw}'.");
    }

    private static bool ReadBoolean(IConfiguration configuration, string key, bool fallback)
    {
        var raw = configuration[$"{ExchangeAdmissionPolicy.SectionName}:{key}"];
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        return bool.TryParse(raw.Trim(), out var parsed)
            ? parsed
            : throw new InvalidOperationException(
                $"{ExchangeAdmissionPolicy.SectionName}:{key} must be true or false; '{raw}' is neither.");
    }
}

/// <summary>A short, non-reversible fingerprint of the value key, safe to log and to pin in configuration.</summary>
public static class AzoaValueKeyFingerprint
{
    /// <summary>Configuration key an operator pins the expected fingerprint under (P6-03 precondition).</summary>
    public const string PinPath = $"{AzoaSettings.SectionName}:ValueKeyFingerprint";

    /// <summary>First 12 hex digits of SHA-256 over the key; empty for an unconfigured key.</summary>
    public static string Of(string? valueApiKey)
        => string.IsNullOrWhiteSpace(valueApiKey)
            ? string.Empty
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valueApiKey.Trim())))[..12].ToLowerInvariant();
}

/// <summary>
/// Boot check for ledger P6-03 (receipts are scoped to the admitting API key): logs the value-key
/// fingerprint, the admission-proof mode and how many settlements are open, and raises Critical when the
/// key no longer matches its pinned fingerprint while any is open. See Implementations/AGENTS.md §Node proof.
/// </summary>
public sealed class AzoaExchangeProofBootCheck : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ExchangeAdmissionPolicy _admission;
    private readonly string _fingerprint;
    private readonly string? _pinnedFingerprint;
    private readonly ILogger<AzoaExchangeProofBootCheck> _logger;

    public AzoaExchangeProofBootCheck(
        IServiceScopeFactory scopeFactory,
        ExchangeAdmissionPolicy admission,
        string fingerprint,
        string? pinnedFingerprint,
        ILogger<AzoaExchangeProofBootCheck> logger)
    {
        _scopeFactory = scopeFactory;
        _admission = admission;
        _fingerprint = fingerprint;
        _pinnedFingerprint = string.IsNullOrWhiteSpace(pinnedFingerprint) ? null : pinnedFingerprint.Trim().ToLowerInvariant();
        _logger = logger;
    }

    /// <summary>Settlements whose next step may read a receipt by key and trust a NOT_FOUND.</summary>
    public static async Task<int> CountOpenAsync(ArdaNovaDbContext context, CancellationToken ct)
        => await context.EconomicSettlements.CountAsync(
            item => item.status == EconomicSettlementStatus.PENDING_DISPATCH
                || item.status == EconomicSettlementStatus.AWAITING_RECONCILIATION,
            ct);

    /// <summary>The verdict, separate from I/O so it is testable: null when there is nothing to say loudly.</summary>
    public static string? Mismatch(string fingerprint, string? pinnedFingerprint, int openSettlements)
        => pinnedFingerprint is not null
            && !string.Equals(fingerprint, pinnedFingerprint, StringComparison.Ordinal)
            && openSettlements > 0
                ? $"The AZOA value key (fingerprint {fingerprint}) does not match {AzoaValueKeyFingerprint.PinPath} "
                    + $"({pinnedFingerprint}) while {openSettlements} settlement(s) are open. Receipts are scoped to the "
                    + "admitting key (P6-03): under a rotated key the node answers NOT_FOUND for exchanges that exist, "
                    + "which the outbox and the void trust as proof nothing was taken. Restore the previous key until "
                    + "no settlement is open, then rotate and re-pin."
                : null;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ArdaNovaDbContext>();
            var open = await CountOpenAsync(context, stoppingToken);

            _logger.LogInformation(
                "AZOA value key fingerprint {Fingerprint}; {OpenSettlements} settlement(s) open. Never rotate the value "
                + "key while any is open (P6-03). Exchange admission: Admit-Before = send stamp + {AdmitSeconds}s, NOT_FOUND "
                + "believed {ProofSeconds}s after the latest send, node enforcement attested: {NodeEnforces}.",
                _fingerprint,
                open,
                _admission.AdmitWindow.TotalSeconds,
                _admission.ProofWindow.TotalSeconds,
                _admission.NodeEnforcesAdmitBefore);

            if (Mismatch(_fingerprint, _pinnedFingerprint, open) is { } problem)
                _logger.LogCritical("{Problem}", problem);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception failure)
        {
            // A boot log must never take the API down; say that the precondition went unchecked.
            _logger.LogWarning(failure, "The AZOA value-key rotation check (P6-03) could not read open settlements; it was not checked this boot.");
        }
    }
}

/// <summary>Registration: one call registers the validated admission policy and the P6-03 boot check.</summary>
public static class AzoaExchangeProofRegistration
{
    public static IServiceCollection AddAzoaExchangeProof(
        this IServiceCollection services,
        IConfiguration configuration,
        string valueApiKey)
    {
        var admission = AzoaExchangeAdmissionSettings.FromConfiguration(configuration);
        services.AddSingleton(admission);

        if (!string.IsNullOrWhiteSpace(valueApiKey))
        {
            var fingerprint = AzoaValueKeyFingerprint.Of(valueApiKey);
            var pinned = configuration[AzoaValueKeyFingerprint.PinPath];
            services.AddHostedService(provider => new AzoaExchangeProofBootCheck(
                provider.GetRequiredService<IServiceScopeFactory>(),
                admission,
                fingerprint,
                pinned,
                provider.GetRequiredService<ILogger<AzoaExchangeProofBootCheck>>()));
        }

        return services;
    }
}
