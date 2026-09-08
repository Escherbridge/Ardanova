namespace ArdaNova.Infrastructure.Azoa;

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// The ONE adapter ArdaNova uses to send a person their money — the withdraw half of cashing out.
/// It calls the node's cash-out routes and nothing else; ArdaNova never holds a bank credential,
/// never terminates a payout webhook, and never names a payment provider in anything a person can
/// read. See <c>Azoa/AGENTS.md</c>.
/// </summary>
/// <remarks>
/// <para>The subject is always the ROUTE value. A caller-supplied destination would be a way to
/// redirect somebody else's money into your own bank, so the node resolves the bank account from
/// that person's own durable onboarding record and this adapter never sends one.</para>
/// <para><b>Why the node's own words are not passed through.</b>
/// <see cref="AzoaNodeClient.SendAsync{T}"/> flattens a refusal to sentences that NAME THE NODE.
/// No person withdrawing their earnings should read a platform's internal name, so every failure
/// is re-worded here from the refusal KIND, exactly as <c>PayoutService.DescribeRefusal</c> does.
/// The node's own text stays where operators read it: the transport log.</para>
/// </remarks>
public sealed class AzoaCashOutClient : IAzoaCashOutNode
{
    private const string PayoutsPrefix = "/api/fiat/payouts/";
    private const int MaxRequestKeyLength = 128;
    private const int MaxHistoryLimit = 100;

    private readonly HttpClient _http;
    private readonly ILogger<AzoaCashOutClient> _logger;

    public AzoaCashOutClient(HttpClient http, ILogger<AzoaCashOutClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<CashOutAvailability>> GetAvailabilityAsync(
        Guid avatarId, CancellationToken ct = default)
    {
        if (avatarId == Guid.Empty)
            return Result<CashOutAvailability>.ValidationError("Reading what you can take out needs an account.");

        var path = $"{PayoutsPrefix}{avatarId:D}/availability";
        var wire = await SendAsync<CashOutQuoteWire>(
            new HttpRequestMessage(HttpMethod.Get, path), IsAvailabilityPath(path), ct);

        if (wire.IsFailure)
            return Refused<CashOutAvailability>(wire.Type);

        var quote = wire.Value!;
        return Result<CashOutAvailability>.Success(new CashOutAvailability(
            quote.Available,
            quote.AccountReady,
            quote.CashableMinorUnits,
            quote.OnHoldMinorUnits,
            quote.AvailableFrom,
            quote.MinimumAmountMinorUnits,
            quote.MaximumAmountMinorUnits,
            NormaliseCurrency(quote.CurrencyCode),
            Sentence(quote.StatusMessage, "We could not check what you can take out just now.")));
    }

    /// <inheritdoc />
    public Task<Result<CashOutDestination>> GetDestinationAsync(
        Guid avatarId, CancellationToken ct = default)
        => DestinationAsync(avatarId, HttpMethod.Get, ct);

    /// <inheritdoc />
    public Task<Result<CashOutDestination>> BeginDestinationSetupAsync(
        Guid avatarId, CancellationToken ct = default)
        => DestinationAsync(avatarId, HttpMethod.Post, ct);

    /// <inheritdoc />
    public async Task<Result<CashOutRecord>> RequestAsync(
        Guid avatarId, string requestKey, long amountMinorUnits, CancellationToken ct = default)
    {
        if (avatarId == Guid.Empty)
            return Result<CashOutRecord>.ValidationError("Sending money needs the account it belongs to.");

        if (!IsAcceptableRequestKey(requestKey))
        {
            return Result<CashOutRecord>.ValidationError(
                "A stable request key of 1-" + MaxRequestKeyLength
                + " plain characters is required so a repeated tap cannot pay twice.");
        }

        if (amountMinorUnits <= 0)
            return Result<CashOutRecord>.ValidationError("The amount to send must be more than nothing.");

        var path = $"{PayoutsPrefix}{avatarId:D}";
        var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            // No destination field, deliberately: see the class remarks.
            Content = JsonContent.Create(new CashOutStartWire
            {
                RequestKey = requestKey.Trim(),
                AmountMinorUnits = amountMinorUnits,
            }),
        };

        var wire = await SendAsync<CashOutViewWire>(message, IsSubjectPath(path), ct);
        return wire.IsFailure
            ? Refused<CashOutRecord>(wire.Type)
            : ToRecord(wire.Value!);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CashOutRecord>>> ListAsync(
        Guid avatarId, int limit, CancellationToken ct = default)
    {
        if (avatarId == Guid.Empty)
            return Result<IReadOnlyList<CashOutRecord>>.ValidationError(
                "Reading a withdrawal history needs an account.");

        // The guard runs on the BARE path: IsSafeApiPath refuses a '?' outright, and the only
        // thing appended after it is an int this method clamped, which cannot carry a separator.
        var path = $"{PayoutsPrefix}{avatarId:D}";
        var bounded = Math.Clamp(limit <= 0 ? 25 : limit, 1, MaxHistoryLimit);
        var request = new HttpRequestMessage(
            HttpMethod.Get, path + "?limit=" + bounded.ToString(CultureInfo.InvariantCulture));

        var wire = await SendAsync<List<CashOutViewWire>>(request, IsSubjectPath(path), ct);
        if (wire.IsFailure)
            return Refused<IReadOnlyList<CashOutRecord>>(wire.Type);

        var records = new List<CashOutRecord>();
        foreach (var row in wire.Value!)
        {
            var mapped = ToRecord(row);
            if (mapped.IsSuccess)
                records.Add(mapped.Value!);
        }

        return Result<IReadOnlyList<CashOutRecord>>.Success(records);
    }

    private async Task<Result<CashOutDestination>> DestinationAsync(
        Guid avatarId, HttpMethod method, CancellationToken ct)
    {
        if (avatarId == Guid.Empty)
            return Result<CashOutDestination>.ValidationError("Reading where your money goes needs an account.");

        var path = $"{PayoutsPrefix}{avatarId:D}/account";
        var wire = await SendAsync<PayoutAccountStatusWire>(
            new HttpRequestMessage(method, path), IsAccountPath(path), ct);

        if (wire.IsFailure)
            return Refused<CashOutDestination>(wire.Type);

        var status = wire.Value!;
        return Result<CashOutDestination>.Success(new CashOutDestination(
            status.Available,
            status.AccountOpened,
            status.ReadyToReceive,
            status.DetailsSubmitted,
            status.OutstandingRequirementCount,
            status.SetupUrl,
            status.SetupUrlExpiresAt,
            Sentence(status.StatusMessage, "We could not check where your money goes just now.")));
    }

    /// <summary>Sends a cash-out call, refusing any path outside the cash-out route family.</summary>
    private Task<Result<T>> SendAsync<T>(HttpRequestMessage message, bool pathAllowed, CancellationToken ct)
    {
        if (!pathAllowed)
        {
            message.Dispose();
            return Task.FromResult(Result<T>.Forbidden(
                "The AZOA tenant credential is restricted to the cash-out endpoints."));
        }

        return AzoaNodeClient.SendAsync<T>(_http, _logger, message, ct);
    }

    private static Result<CashOutRecord> ToRecord(CashOutViewWire row)
    {
        // A cash-out with no identity cannot be followed up, and an invented one would read to
        // the person as a payment that exists. Both are refused rather than filled in.
        if (string.IsNullOrWhiteSpace(row.Id) || string.IsNullOrWhiteSpace(row.Status))
        {
            return Result<CashOutRecord>.Conflict(
                "We could not confirm this withdrawal just now. Support has been notified.");
        }

        return Result<CashOutRecord>.Success(new CashOutRecord(
            row.Id!.Trim(),
            row.Status!.Trim(),
            Sentence(row.StatusMessage, "This withdrawal is being looked at."),
            row.AmountMinorUnits,
            NormaliseCurrency(row.CurrencyCode),
            row.RequestedAt,
            row.PaidAt,
            row.ReversedAt));
    }

    /// <summary>ArdaNova's own words for a refusal, chosen by KIND. See the class remarks.</summary>
    private static Result<T> Refused<T>(ResultType type) => type switch
    {
        ResultType.NotFound => Result<T>.NotFound(
            "We could not find this withdrawal, so nothing was sent."),
        ResultType.Forbidden or ResultType.Unauthorized => Result<T>.Forbidden(
            "Your account is not set up to receive money yet, so nothing was sent. "
            + "Finish setting up where your money goes and try again."),
        ResultType.ValidationError or ResultType.BadRequest => Result<T>.BadRequest(
            "This withdrawal could not be made on those terms, so nothing was sent."),
        // No promise that this finishes by itself: nothing on a timer chases a parked withdrawal.
        ResultType.Conflict => Result<T>.Conflict(
            "We could not confirm this withdrawal just now, so it stays open. "
            + "Support checks open withdrawals; nothing more is needed from you."),
        _ => Result<T>.Failure(
            "We could not reach the service that sends your money. Nothing was sent — try again shortly."),
    };

    /// <summary>GET/POST target: exactly one person's payout account.</summary>
    private static bool IsAccountPath(string? path)
        => IsSubjectScopedPath(path, "/account");

    /// <summary>GET target: exactly one person's cash-out availability.</summary>
    private static bool IsAvailabilityPath(string? path)
        => IsSubjectScopedPath(path, "/availability");

    /// <summary>GET/POST target: exactly one person's cash-out collection.</summary>
    private static bool IsSubjectPath(string? path)
        => IsSubjectScopedPath(path, suffix: null);

    private static bool IsSubjectScopedPath(string? path, string? suffix)
    {
        if (!AzoaNodePathPolicy.IsSafeApiPath(path)
            || !path!.StartsWith(PayoutsPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var remainder = path[PayoutsPrefix.Length..];
        if (suffix is not null)
        {
            if (!remainder.EndsWith(suffix, StringComparison.Ordinal))
                return false;
            remainder = remainder[..^suffix.Length];
        }

        return Guid.TryParseExact(remainder, "D", out _);
    }

    /// <summary>The node's own key alphabet, applied before a key can reach a request body.</summary>
    private static bool IsAcceptableRequestKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        var trimmed = key.Trim();
        if (trimmed.Length > MaxRequestKeyLength)
            return false;

        foreach (var character in trimmed)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_' or ':' or '.'))
                return false;
        }

        return true;
    }

    private static string NormaliseCurrency(string? code)
        => string.IsNullOrWhiteSpace(code) ? "USD" : code.Trim().ToUpperInvariant();

    private static string Sentence(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}

/// <summary>Wire shape of a cash-out claim. The payer is the route value, never a field here.</summary>
internal sealed class CashOutStartWire
{
    [JsonPropertyName("requestKey")] public string RequestKey { get; set; } = string.Empty;
    [JsonPropertyName("amountMinorUnits")] public long AmountMinorUnits { get; set; }
}

/// <summary>Wire shape of what a person can cash out today, as the node reports it.</summary>
internal sealed class CashOutQuoteWire
{
    [JsonPropertyName("available")] public bool Available { get; set; }
    [JsonPropertyName("accountReady")] public bool AccountReady { get; set; }
    [JsonPropertyName("cashableMinorUnits")] public long CashableMinorUnits { get; set; }
    [JsonPropertyName("onHoldMinorUnits")] public long OnHoldMinorUnits { get; set; }
    [JsonPropertyName("availableFrom")] public DateTimeOffset? AvailableFrom { get; set; }
    [JsonPropertyName("minimumAmountMinorUnits")] public long MinimumAmountMinorUnits { get; set; }
    [JsonPropertyName("maximumAmountMinorUnits")] public long MaximumAmountMinorUnits { get; set; }
    [JsonPropertyName("currencyCode")] public string? CurrencyCode { get; set; }
    [JsonPropertyName("statusMessage")] public string? StatusMessage { get; set; }
}

/// <summary>Wire shape of where a person's money goes, as the node reports it.</summary>
internal sealed class PayoutAccountStatusWire
{
    [JsonPropertyName("available")] public bool Available { get; set; }
    [JsonPropertyName("accountOpened")] public bool AccountOpened { get; set; }
    [JsonPropertyName("readyToReceive")] public bool ReadyToReceive { get; set; }
    [JsonPropertyName("detailsSubmitted")] public bool DetailsSubmitted { get; set; }
    [JsonPropertyName("outstandingRequirementCount")] public int OutstandingRequirementCount { get; set; }
    [JsonPropertyName("setupUrl")] public string? SetupUrl { get; set; }
    [JsonPropertyName("setupUrlExpiresAt")] public DateTimeOffset? SetupUrlExpiresAt { get; set; }
    [JsonPropertyName("statusMessage")] public string? StatusMessage { get; set; }
}

/// <summary>Wire shape of one cash-out as the node reports it.</summary>
internal sealed class CashOutViewWire
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("statusMessage")] public string? StatusMessage { get; set; }
    [JsonPropertyName("amountMinorUnits")] public long AmountMinorUnits { get; set; }
    [JsonPropertyName("currencyCode")] public string? CurrencyCode { get; set; }
    [JsonPropertyName("requestedAt")] public DateTimeOffset RequestedAt { get; set; }
    [JsonPropertyName("paidAt")] public DateTimeOffset? PaidAt { get; set; }
    [JsonPropertyName("reversedAt")] public DateTimeOffset? ReversedAt { get; set; }
}

/// <summary>
/// Registration for the cash-out adapter, kept here so wiring it costs exactly one call in
/// <c>Infrastructure.DependencyInjection</c> — the same arrangement the on-ramp adapter uses.
/// </summary>
public static class AzoaCashOutClientRegistration
{
    /// <summary>
    /// Registers the cash-out transport and the <see cref="IAzoaCashOutNode"/> binding it
    /// satisfies. The binding is added ONLY when the node transport actually resolves; without a
    /// base URL and a custody credential nothing is registered, and <c>PayoutService</c>'s own
    /// fail-closed path reports that cashing out is not available rather than throwing at the
    /// first withdrawal.
    /// </summary>
    public static IServiceCollection AddAzoaCashOut(
        this IServiceCollection services,
        IConfiguration configuration,
        string? environmentName = null)
    {
        var settings = configuration.GetSection(AzoaSettings.SectionName).Get<AzoaSettings>()
            ?? new AzoaSettings();
        var isProduction = string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase);
        var custodyApiKey = AzoaCredentialSelection.ResolveCustodyKey(settings, isProduction);
        var transportConfigured =
            Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _)
            && !string.IsNullOrWhiteSpace(custodyApiKey);

        services.AddHttpClient<AzoaCashOutClient>(client =>
        {
            if (Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseUri))
                client.BaseAddress = baseUri;
            if (!string.IsNullOrWhiteSpace(custodyApiKey))
                client.DefaultRequestHeaders.Add("X-Api-Key", custodyApiKey);
            client.Timeout = TimeSpan.FromSeconds(
                settings.TimeoutSeconds > 0 ? settings.TimeoutSeconds : 30);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            // A redirect off the node is a redirect of somebody's bank details.
            AllowAutoRedirect = false,
        });

        if (!transportConfigured)
            return services;

        services.AddScoped<IAzoaCashOutNode>(provider => provider.GetRequiredService<AzoaCashOutClient>());
        return services;
    }
}
