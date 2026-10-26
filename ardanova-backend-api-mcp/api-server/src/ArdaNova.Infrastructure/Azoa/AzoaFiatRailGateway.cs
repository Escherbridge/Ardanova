namespace ArdaNova.Infrastructure.Azoa;

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// The ONE adapter ArdaNova uses to add funds. It calls the node's <c>Fiat.USD</c> rail and
/// nothing else; ArdaNova never holds a card credential, never terminates a payment webhook, and
/// never names a payment provider in anything a person can read.
/// </summary>
/// <remarks>
/// The credential is the tenant custody key, because the node's checkout route authorises an
/// API-key principal through the tenant-provision scope plus the person's live consent grant. The
/// path guard below restricts that key to the two fiat route families, the same route-family
/// restriction the other typed clients in this directory apply (see AGENTS.md).
/// </remarks>
public sealed class AzoaFiatRailGateway : IFiatRailGateway
{
    /// <summary>The node's own routing value for "add spendable funds to the payer's account".</summary>
    private const string FundsTopUpPurpose = "funds-top-up";

    private const string CheckoutPrefix = "/api/fiat/checkout/";
    private const string PriceSchedulePath = "/api/fiat/price-schedule";
    private const int MaxRequestKeyLength = 128;

    private readonly HttpClient _http;
    private readonly ILogger<AzoaFiatRailGateway> _logger;
    private readonly ArdaOnRampOptions _options;

    public AzoaFiatRailGateway(
        HttpClient http,
        ILogger<AzoaFiatRailGateway> logger,
        ArdaOnRampOptions options)
    {
        _http = http;
        _logger = logger;
        _options = options;
    }

    /// <inheritdoc />
    public Task<Result<FiatCheckoutSession>> BeginAddFundsAsync(
        FiatCheckoutStart request, CancellationToken ct = default)
    {
        if (request is null)
            return Refused("An add-funds request is required.");

        if (request.AccountId == Guid.Empty)
            return Refused("Adding funds needs the account it belongs to.");

        if (!IsAcceptableRequestKey(request.RequestKey))
        {
            return Refused(
                "A stable request key of 1-" + MaxRequestKeyLength
                + " plain characters is required so a repeated tap cannot pay twice.");
        }

        if (request.AmountMinorUnits < _options.MinimumAmountMinorUnits)
        {
            return Refused(
                $"The smallest amount you can add is {FormatMoney(_options.MinimumAmountMinorUnits)}.");
        }

        if (request.AmountMinorUnits > _options.MaximumAmountMinorUnits)
        {
            return Refused(
                $"The most you can add in one go is {FormatMoney(_options.MaximumAmountMinorUnits)}.");
        }

        if (!IsAcceptableReturnUrl(request.ReturnUrl) || !IsAcceptableReturnUrl(request.CancelUrl))
            return Refused("The pages to return to must be absolute http(s) addresses.");

        var path = CheckoutPrefix + request.AccountId.ToString("D");
        var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new AzoaFiatCheckoutStartWire
            {
                RequestKey = request.RequestKey.Trim(),
                Purpose = FundsTopUpPurpose,
                AmountMinorUnits = request.AmountMinorUnits,
                SuccessUrl = request.ReturnUrl,
                CancelUrl = request.CancelUrl,
            }),
        };

        return SendIntentAsync(message, IsCheckoutStartPath(path), ct);
    }

    /// <inheritdoc />
    public Task<Result<FiatCheckoutSession>> GetSessionAsync(
        Guid accountId, Guid intentId, CancellationToken ct = default)
    {
        if (accountId == Guid.Empty || intentId == Guid.Empty)
            return Refused("Reading a payment needs both the account and the payment it belongs to.");

        var path = $"{CheckoutPrefix}{accountId:D}/{intentId:D}";
        var message = new HttpRequestMessage(HttpMethod.Get, path);

        return SendIntentAsync(message, IsCheckoutReadPath(path), ct);
    }

    /// <summary>
    /// Declares, once, the price at which money buys ARDA on this node (Q-X2). The node derives
    /// the ARDA quantity from the cleared amount times THIS registered row, so an unregistered
    /// price means the node issues nothing rather than guessing.
    /// </summary>
    /// <remarks>
    /// A price already registered at a DIFFERENT value is left alone unless the operator opted in
    /// with <c>Azoa:OnRamp:AllowPriceUpdate</c>. ARDA is backing-derived and floats; silently
    /// re-registering the genesis seed on every restart would re-peg it behind the operator's back.
    /// </remarks>
    public async Task<Result<string>> RegisterPriceScheduleAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return Result<string>.Success("The ARDA on-ramp is switched off; no price was registered.");

        if (!IsPathSafeSegment(_options.ChainType) || !IsPathSafeSegment(_options.AssetId))
        {
            return Result<string>.ValidationError(
                "Registering the on-ramp price needs Azoa:OnRamp:AssetId and Azoa:ChainType set to "
                + "plain identifiers.");
        }

        if (!IsExactWholeNumber(_options.UnitsPerUsdMinorUnit))
        {
            return Result<string>.ValidationError(
                "Azoa:OnRamp:UnitsPerUsdMinorUnit must be a whole number of base units, as a string.");
        }

        var readPath = $"{PriceSchedulePath}/{_options.ChainType}/{_options.AssetId}";
        var current = await SendScheduleAsync(
            new HttpRequestMessage(HttpMethod.Get, readPath), IsPriceScheduleReadPath(readPath), ct);

        if (current.IsSuccess && current.Value is { } registered)
        {
            var unchanged = string.Equals(
                NormaliseWholeNumber(registered.UnitsPerMinorUnit),
                NormaliseWholeNumber(_options.UnitsPerUsdMinorUnit),
                StringComparison.Ordinal);

            if (unchanged && string.Equals(registered.Status, "Active", StringComparison.OrdinalIgnoreCase))
                return Result<string>.Success($"The on-ramp price is already registered at version {registered.Version}.");

            if (!_options.AllowPriceUpdate)
            {
                _logger.LogWarning(
                    "The node's registered ARDA on-ramp price ({Registered}, {Status}) differs from this "
                    + "deployment's configured price ({Configured}). It was left unchanged; set "
                    + "Azoa:OnRamp:AllowPriceUpdate to overwrite it deliberately.",
                    registered.UnitsPerMinorUnit,
                    registered.Status,
                    _options.UnitsPerUsdMinorUnit);
                return Result<string>.Conflict(
                    "A different on-ramp price is already registered on the node; it was left unchanged.");
            }

            return await WriteScheduleAsync(registered.Version, ct);
        }

        if (current.Type != ResultType.NotFound)
            return Result<string>.Failure(current.Error ?? "The registered on-ramp price could not be read.");

        return await WriteScheduleAsync(expectedCurrentVersion: null, ct);
    }

    private async Task<Result<string>> WriteScheduleAsync(long? expectedCurrentVersion, CancellationToken ct)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, PriceSchedulePath)
        {
            Content = JsonContent.Create(new AzoaPriceScheduleWriteWire
            {
                ChainType = _options.ChainType,
                AssetId = _options.AssetId,
                CurrencyCode = "USD",
                UnitsPerMinorUnit = NormaliseWholeNumber(_options.UnitsPerUsdMinorUnit),
                Source = _options.PriceSource,
                Note = _options.PriceNote,
                DeclaredAt = DateTimeOffset.UtcNow,
                ExpectedCurrentVersion = expectedCurrentVersion,
                Retire = false,
            }),
        };

        var written = await SendScheduleAsync(message, IsPriceScheduleWritePath(PriceSchedulePath), ct);
        return written.IsSuccess && written.Value is { } schedule
            ? Result<string>.Success($"Registered the on-ramp price as version {schedule.Version}.")
            : Result<string>.Failure(written.Error ?? "The on-ramp price could not be registered.");
    }

    /// <summary>Sends a checkout call, refusing any path outside the checkout route family.</summary>
    private async Task<Result<FiatCheckoutSession>> SendIntentAsync(
        HttpRequestMessage message, bool pathAllowed, CancellationToken ct)
    {
        if (!pathAllowed)
        {
            message.Dispose();
            return Result<FiatCheckoutSession>.Forbidden(
                "The AZOA tenant credential is restricted to the fiat checkout endpoints.");
        }

        var wire = await AzoaNodeClient.SendAsync<AzoaFiatCheckoutIntentWire>(_http, _logger, message, ct);
        if (wire.IsFailure)
            return MapFailure<AzoaFiatCheckoutIntentWire, FiatCheckoutSession>(wire);

        var intent = wire.Value!;

        // A payment with no identity or no state cannot be followed up, and an invented state
        // would read to the payer as progress. Both are refused rather than filled in.
        if (intent.IntentId == Guid.Empty || string.IsNullOrWhiteSpace(intent.Status))
        {
            return Result<FiatCheckoutSession>.Conflict(
                "The node returned a payment it could not describe, so it cannot be followed up.");
        }

        return Result<FiatCheckoutSession>.Success(new FiatCheckoutSession(
            intent.IntentId,
            intent.Status,
            intent.SettlementState ?? string.Empty,
            intent.PaymentUrl,
            intent.Resumed,
            intent.AmountPaidMinorUnits,
            intent.Currency,
            intent.ExpiresAt));
    }

    /// <summary>Sends a price-schedule call, refusing any path outside that route family.</summary>
    private async Task<Result<AzoaPriceScheduleWire>> SendScheduleAsync(
        HttpRequestMessage message, bool pathAllowed, CancellationToken ct)
    {
        if (!pathAllowed)
        {
            message.Dispose();
            return Result<AzoaPriceScheduleWire>.Forbidden(
                "The AZOA tenant credential is restricted to the fiat price-schedule endpoints.");
        }

        return await AzoaNodeClient.SendAsync<AzoaPriceScheduleWire>(_http, _logger, message, ct);
    }

    private static Task<Result<FiatCheckoutSession>> Refused(string message)
        => Task.FromResult(Result<FiatCheckoutSession>.ValidationError(message));

    /// <summary>POST target: the checkout route for exactly one account.</summary>
    private static bool IsCheckoutStartPath(string? path)
        => AzoaNodePathPolicy.IsSafeApiPath(path)
            && path!.StartsWith(CheckoutPrefix, StringComparison.Ordinal)
            && Guid.TryParseExact(path[CheckoutPrefix.Length..], "D", out _);

    /// <summary>GET target: one payment belonging to exactly one account.</summary>
    private static bool IsCheckoutReadPath(string? path)
    {
        if (!AzoaNodePathPolicy.IsSafeApiPath(path)
            || !path!.StartsWith(CheckoutPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var segments = path[CheckoutPrefix.Length..].Split('/');
        return segments.Length == 2
            && Guid.TryParseExact(segments[0], "D", out _)
            && Guid.TryParseExact(segments[1], "D", out _);
    }

    /// <summary>POST target: the caller's own price schedule, and nothing else.</summary>
    private static bool IsPriceScheduleWritePath(string? path)
        => AzoaNodePathPolicy.IsSafeApiPath(path)
            && string.Equals(path, PriceSchedulePath, StringComparison.Ordinal);

    /// <summary>GET target: one registered price for one asset on one chain.</summary>
    private static bool IsPriceScheduleReadPath(string? path)
    {
        if (!AzoaNodePathPolicy.IsSafeApiPath(path)
            || !path!.StartsWith(PriceSchedulePath + "/", StringComparison.Ordinal))
        {
            return false;
        }

        var segments = path[(PriceSchedulePath.Length + 1)..].Split('/');
        return segments.Length == 2
            && IsPathSafeSegment(segments[0])
            && IsPathSafeSegment(segments[1]);
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

    /// <summary>
    /// An absolute http(s) address with no credentials and no fragment. The node origin-allowlists
    /// these as well; refusing here first turns a misconfiguration into a readable message rather
    /// than a rejected payment.
    /// </summary>
    private static bool IsAcceptableReturnUrl(string? value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            && string.IsNullOrEmpty(uri.UserInfo)
            && string.IsNullOrEmpty(uri.Fragment);

    private static bool IsPathSafeSegment(string? value)
        => !string.IsNullOrWhiteSpace(value)
            && value.All(character =>
                char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');

    private static bool IsExactWholeNumber(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length is > 0 and <= 40
            && trimmed.All(char.IsAsciiDigit)
            && trimmed.TrimStart('0').Length > 0;
    }

    private static string NormaliseWholeNumber(string? value)
    {
        var withoutLeadingZeroes = (value ?? string.Empty).Trim().TrimStart('0');
        return withoutLeadingZeroes.Length == 0 ? "0" : withoutLeadingZeroes;
    }

    private static string FormatMoney(long minorUnits)
        => (minorUnits / 100m).ToString("C2", CultureInfo.GetCultureInfo("en-US"));

    private static Result<TOut> MapFailure<TIn, TOut>(Result<TIn> source)
    {
        var error = source.Error ?? "The AZOA fiat rail refused the request.";
        return source.Type switch
        {
            ResultType.NotFound => Result<TOut>.NotFound(error),
            ResultType.Forbidden => Result<TOut>.Forbidden(error),
            ResultType.Unauthorized => Result<TOut>.Unauthorized(error),
            ResultType.Conflict => Result<TOut>.Conflict(error),
            ResultType.ValidationError => Result<TOut>.ValidationError(error),
            ResultType.BadRequest => Result<TOut>.BadRequest(error),
            _ => Result<TOut>.Failure(error),
        };
    }
}

/// <summary>Deployment settings for the ARDA on-ramp. Section <c>Azoa:OnRamp</c>.</summary>
public sealed class ArdaOnRampOptions
{
    public const string SectionName = "Azoa:OnRamp";

    /// <summary>Master switch. Off means the seam stays bound to its fail-closed refusal.</summary>
    public bool Enabled { get; init; }

    /// <summary>The chain the priced asset lives on, as the node's provider spells it.</summary>
    public string ChainType { get; init; } = "Algorand";

    /// <summary>The ARDA asset the money buys. Required; blank means nothing is registered.</summary>
    public string AssetId { get; init; } = string.Empty;

    /// <summary>
    /// Base units of ARDA that ONE cent buys, as a whole number string. The genesis seed is
    /// "100": at two decimals that is 1 ARDA per cent, i.e. 1 ARDA = $0.01. The live rate is
    /// backing-derived and floats from there, which is why this is registered once, not per tick.
    /// </summary>
    public string UnitsPerUsdMinorUnit { get; init; } = "100";

    /// <summary>The registrant's own name for where this price came from.</summary>
    public string PriceSource { get; init; } = "ardanova.genesis-seed";

    /// <summary>Free-text note stored with the price version. Never interpreted.</summary>
    public string? PriceNote { get; init; }

    /// <summary>Opt-in to overwrite a differing price already registered on the node.</summary>
    public bool AllowPriceUpdate { get; init; }

    /// <summary>Smallest amount a person may add in one payment. Default $1.00.</summary>
    public long MinimumAmountMinorUnits { get; init; } = 100;

    /// <summary>Largest amount a person may add in one payment. Default $10,000.00.</summary>
    public long MaximumAmountMinorUnits { get; init; } = 1_000_000;

    /// <summary>How long the startup price registration may take before it is abandoned.</summary>
    public int RegistrationTimeoutSeconds { get; init; } = 15;

    /// <summary>Binds the section, keeping every default above when a key is absent.</summary>
    public static ArdaOnRampOptions Bind(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var defaults = new ArdaOnRampOptions();
        return new ArdaOnRampOptions
        {
            Enabled = section.GetValue("Enabled", defaults.Enabled),
            ChainType = FirstConfigured(
                section["ChainType"],
                configuration[$"{AzoaSettings.SectionName}:ChainType"],
                defaults.ChainType),
            AssetId = section["AssetId"]?.Trim() ?? defaults.AssetId,
            UnitsPerUsdMinorUnit = FirstConfigured(
                section["UnitsPerUsdMinorUnit"], null, defaults.UnitsPerUsdMinorUnit),
            PriceSource = FirstConfigured(section["Source"], null, defaults.PriceSource),
            PriceNote = section["Note"],
            AllowPriceUpdate = section.GetValue("AllowPriceUpdate", defaults.AllowPriceUpdate),
            MinimumAmountMinorUnits = section.GetValue(
                "MinimumAmountMinorUnits", defaults.MinimumAmountMinorUnits),
            MaximumAmountMinorUnits = section.GetValue(
                "MaximumAmountMinorUnits", defaults.MaximumAmountMinorUnits),
            RegistrationTimeoutSeconds = section.GetValue(
                "RegistrationTimeoutSeconds", defaults.RegistrationTimeoutSeconds),
        };
    }

    private static string FirstConfigured(string? first, string? second, string last)
    {
        if (!string.IsNullOrWhiteSpace(first)) return first.Trim();
        if (!string.IsNullOrWhiteSpace(second)) return second.Trim();
        return last;
    }
}

/// <summary>
/// Declares this deployment's on-ramp price to the node once, after boot. It never blocks
/// startup and never throws: an unregistered price makes the node's on-ramp issue nothing, which
/// is the fail-closed outcome, so a loud log is the right escalation rather than a crash loop.
/// </summary>
public sealed class ArdaOnRampPriceScheduleRegistrar : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ArdaOnRampPriceScheduleRegistrar> _logger;
    private readonly ArdaOnRampOptions _options;

    public ArdaOnRampPriceScheduleRegistrar(
        IServiceScopeFactory scopes,
        ILogger<ArdaOnRampPriceScheduleRegistrar> logger,
        ArdaOnRampOptions options)
    {
        _scopes = scopes;
        _logger = logger;
        _options = options;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "The ARDA on-ramp is switched off (Azoa:OnRamp:Enabled), so no price was registered.");
            return;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(
            _options.RegistrationTimeoutSeconds > 0 ? _options.RegistrationTimeoutSeconds : 15));

        try
        {
            using var scope = _scopes.CreateScope();
            var gateway = scope.ServiceProvider.GetRequiredService<AzoaFiatRailGateway>();
            var result = await gateway.RegisterPriceScheduleAsync(timeout.Token);

            if (result.IsSuccess)
                _logger.LogInformation("ARDA on-ramp price: {Outcome}", result.Value);
            else
                _logger.LogError("ARDA on-ramp price was NOT registered: {Reason}", result.Error);
        }
        catch (OperationCanceledException)
        {
            _logger.LogError(
                "ARDA on-ramp price registration did not finish within {TimeoutSeconds}s; "
                + "adding funds will not issue ARDA until it does.",
                _options.RegistrationTimeoutSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ARDA on-ramp price registration failed.");
        }
    }
}

/// <summary>Wire shape of an add-funds start. The payer is the route value, never a field here.</summary>
internal sealed class AzoaFiatCheckoutStartWire
{
    [JsonPropertyName("requestKey")] public string RequestKey { get; set; } = string.Empty;
    [JsonPropertyName("purpose")] public string Purpose { get; set; } = string.Empty;
    [JsonPropertyName("amountMinorUnits")] public long AmountMinorUnits { get; set; }
    [JsonPropertyName("successUrl")] public string? SuccessUrl { get; set; }
    [JsonPropertyName("cancelUrl")] public string? CancelUrl { get; set; }
}

/// <summary>Wire shape of one payment as the node reports it.</summary>
internal sealed class AzoaFiatCheckoutIntentWire
{
    [JsonPropertyName("intentId")] public Guid IntentId { get; set; }
    [JsonPropertyName("admissionKey")] public string? AdmissionKey { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("settlementState")] public string? SettlementState { get; set; }
    [JsonPropertyName("paymentUrl")] public string? PaymentUrl { get; set; }
    [JsonPropertyName("resumed")] public bool Resumed { get; set; }
    [JsonPropertyName("amountPaidMinorUnits")] public long? AmountPaidMinorUnits { get; set; }
    [JsonPropertyName("currency")] public string? Currency { get; set; }
    [JsonPropertyName("expiresAt")] public DateTimeOffset? ExpiresAt { get; set; }
}

/// <summary>Wire shape of a price registration. Exact whole-number strings, never floats.</summary>
internal sealed class AzoaPriceScheduleWriteWire
{
    [JsonPropertyName("chainType")] public string ChainType { get; set; } = string.Empty;
    [JsonPropertyName("assetId")] public string AssetId { get; set; } = string.Empty;
    [JsonPropertyName("currencyCode")] public string CurrencyCode { get; set; } = "USD";
    [JsonPropertyName("unitsPerMinorUnit")] public string UnitsPerMinorUnit { get; set; } = string.Empty;
    [JsonPropertyName("source")] public string Source { get; set; } = string.Empty;
    [JsonPropertyName("note")] public string? Note { get; set; }
    [JsonPropertyName("declaredAt")] public DateTimeOffset? DeclaredAt { get; set; }
    [JsonPropertyName("expectedCurrentVersion")] public long? ExpectedCurrentVersion { get; set; }
    [JsonPropertyName("retire")] public bool Retire { get; set; }
}

/// <summary>Wire shape of one registered price version as the node returns it.</summary>
internal sealed class AzoaPriceScheduleWire
{
    [JsonPropertyName("chainType")] public string? ChainType { get; set; }
    [JsonPropertyName("assetId")] public string? AssetId { get; set; }
    [JsonPropertyName("currencyCode")] public string? CurrencyCode { get; set; }
    [JsonPropertyName("version")] public long Version { get; set; }
    [JsonPropertyName("unitsPerMinorUnit")] public string? UnitsPerMinorUnit { get; set; }
    [JsonPropertyName("source")] public string? Source { get; set; }
    [JsonPropertyName("note")] public string? Note { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("declaredAt")] public DateTimeOffset DeclaredAt { get; set; }
    [JsonPropertyName("registeredAt")] public DateTimeOffset RegisteredAt { get; set; }
}

/// <summary>The genesis ARDA/USD price the registrar declares, as exact USD per whole ARDA.</summary>
/// <remarks>See <c>ArdaNova.Application/Services/Implementations/AGENTS.md</c> section exchange-pricing.</remarks>
public sealed class OnRampArdaGenesisPrice : IArdaGenesisPrice
{
    private const decimal ArdaBaseUnitsPerWholeArda =
        ArdaNova.Application.Services.Implementations.Money.MinorUnitsPerWholeUnit;
    private const decimal DollarsPerCent = 0.01m;

    /// <summary>Reads the same <see cref="ArdaOnRampOptions"/> the registrar declares to the node.</summary>
    public OnRampArdaGenesisPrice(ArdaOnRampOptions options)
    {
        UsdPerWholeArda = options.Enabled
            && !string.IsNullOrWhiteSpace(options.AssetId)
            && long.TryParse(options.UnitsPerUsdMinorUnit, NumberStyles.None, CultureInfo.InvariantCulture, out var unitsPerCent)
            && unitsPerCent > 0
                ? ArdaBaseUnitsPerWholeArda / unitsPerCent * DollarsPerCent
                : null;
    }

    /// <inheritdoc />
    public decimal? UsdPerWholeArda { get; }
}

/// <summary>
/// Registration for the on-ramp adapter, kept here so wiring it costs exactly one call in
/// <c>Infrastructure.DependencyInjection</c> - the same arrangement the exchange adapter uses.
/// </summary>
public static class AzoaFiatRailGatewayRegistration
{
    /// <summary>
    /// Registers the fiat-rail transport, the <see cref="IFiatRailGateway"/> binding it satisfies,
    /// and the startup price registration. When the on-ramp is switched off or the node transport
    /// is not configured, the fail-closed <see cref="DisabledFiatRailGateway"/> that
    /// <c>AddApplication</c> registered is left in place instead.
    /// </summary>
    public static IServiceCollection AddAzoaFiatRail(
        this IServiceCollection services,
        IConfiguration configuration,
        string? environmentName = null)
    {
        var onRamp = ArdaOnRampOptions.Bind(configuration);
        services.AddSingleton(onRamp);

        var settings = configuration.GetSection(AzoaSettings.SectionName).Get<AzoaSettings>()
            ?? new AzoaSettings();
        var isProduction = string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase);
        var custodyApiKey = AzoaCredentialSelection.ResolveCustodyKey(settings, isProduction);
        var transportConfigured =
            Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _)
            && !string.IsNullOrWhiteSpace(custodyApiKey);

        services.AddHttpClient<AzoaFiatRailGateway>(client =>
        {
            if (Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseUri))
                client.BaseAddress = baseUri;
            if (!string.IsNullOrWhiteSpace(custodyApiKey))
                client.DefaultRequestHeaders.Add("X-Api-Key", custodyApiKey);
            client.Timeout = TimeSpan.FromSeconds(
                settings.TimeoutSeconds > 0 ? settings.TimeoutSeconds : 30);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
        });

        // Two switches, one answer: the operator has to opt in AND the transport has to exist
        // before a person can be sent to a payment page.
        if (!onRamp.Enabled || !transportConfigured)
            return services;

        services.AddScoped<IFiatRailGateway>(provider => provider.GetRequiredService<AzoaFiatRailGateway>());
        services.AddHostedService<ArdaOnRampPriceScheduleRegistrar>();
        // The price a genesis treasury quotes at is the one this registrar declares, never a second copy.
        services.AddSingleton<IArdaGenesisPrice>(new OnRampArdaGenesisPrice(onRamp));
        return services;
    }
}
