namespace ArdaNova.Infrastructure.Azoa;

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// The ONE adapter every ArdaNova caller uses for exchange operations against the node.
/// </summary>
/// <remarks>
/// It owns the exchange wire shape so no service assembles a node request of its own, and it
/// carries the scoped value credential to <c>/api/exchange</c> only - the same route-family
/// restriction the other typed clients apply (see this directory's AGENTS.md).
/// <para>
/// <b>Amounts are opaque strings on the wire.</b> ArdaNova has already decided them in base
/// units; the node derives no economics (AZOA contract sections 1 and 3). The declared rate
/// travels as an exact decimal string because the node REFUSES a price it cannot represent
/// exactly rather than rounding it.
/// </para>
/// <para>
/// <b>STUB - balance reads.</b> <see cref="IAzoaProjectTreasuryReadModel"/> and
/// <see cref="IAzoaHoldingsReadModel"/> are implemented here as fail-closed refusals because
/// the node exposes no balance read an API-key principal can reach:
/// <c>GET /api/admin/project-treasuries</c> is behind the Operator policy (JWT plus an operator
/// capability - an API key can never reach it) and <c>GET /api/wallet/{id}/portfolio</c> scopes
/// to the calling principal's own wallets, not a tenant child user's. Both refuse with a named
/// reason; NEITHER returns a fabricated or database-sourced number. Unblocked by an
/// API-key-reachable consumer read on AZOA - see <see cref="UnblockedBy"/>.
/// </para>
/// </remarks>
public sealed class AzoaExchangeClient
    : IAzoaExchangeNode, IAzoaProjectTreasuryReadModel, IAzoaHoldingsReadModel
{
    /// <summary>What has to exist on the node before the balance reads below can stop refusing.</summary>
    public const string UnblockedBy =
        "AZOA must expose a consumer balance read an API-key principal can reach: a tenant-scoped "
        + "project-treasury balance route (today only Operator-policy /api/admin/project-treasuries) "
        + "and a tenant-scoped holdings route for a child user (today /api/wallet/{id}/portfolio is "
        + "scoped to the caller's own wallets).";

    private const string ExchangePrefix = "/api/exchange/";
    private const string ReceiptPrefix = "/api/exchange/receipt/";
    private const int MaxIdempotencyKeyLength = 128;

    private readonly HttpClient _http;
    private readonly ILogger<AzoaExchangeClient> _logger;
    private readonly AzoaSettings _settings;

    public AzoaExchangeClient(
        HttpClient http,
        ILogger<AzoaExchangeClient> logger,
        IOptions<AzoaSettings> settings)
    {
        _http = http;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public Task<Result<ArdaNovaExchangeReceipt>> ExchangeAsync(
        Guid avatarId,
        ArdaNovaExchangeCommand command,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        if (avatarId == Guid.Empty)
            return Task.FromResult(Result<ArdaNovaExchangeReceipt>.ValidationError(
                "An exchange needs the recipient's avatar; it is the route value, never a body field."));

        if (!IsAcceptableIdempotencyKey(idempotencyKey))
            return Task.FromResult(Result<ArdaNovaExchangeReceipt>.ValidationError(
                "A stable Idempotency-Key of 1-" + MaxIdempotencyKeyLength
                + " characters is required for every exchange."));

        if (command.PlatformAmountBaseUnits <= 0 || command.ProjectUnitAmountBaseUnits <= 0)
            return Task.FromResult(Result<ArdaNovaExchangeReceipt>.ValidationError(
                "Both legs of an exchange must carry a positive amount."));

        if (string.IsNullOrWhiteSpace(command.ProjectReference)
            || string.IsNullOrWhiteSpace(command.ProjectAssetId)
            || string.IsNullOrWhiteSpace(command.ReservePartition))
        {
            return Task.FromResult(Result<ArdaNovaExchangeReceipt>.ValidationError(
                "An exchange needs the project, its asset and the reserve partition it draws from."));
        }

        var path = ExchangePrefix + avatarId.ToString("D");
        var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(ToWire(command)),
        };
        message.Headers.Add("Idempotency-Key", idempotencyKey.Trim());

        return SendAsync(message, IsExchangePath(path), ct);
    }

    /// <inheritdoc />
    /// <remarks>NotFound only for the node's own NOT_FOUND envelope; any other 404 is an ambiguous failure.</remarks>
    public Task<Result<ArdaNovaExchangeReceipt>> GetReceiptAsync(
        string idempotencyKey,
        CancellationToken ct = default)
    {
        if (!IsAcceptableIdempotencyKey(idempotencyKey))
            return Task.FromResult(Result<ArdaNovaExchangeReceipt>.ValidationError(
                "A receipt is read by the idempotency key the exchange was submitted with."));

        var path = ReceiptPrefix + idempotencyKey.Trim();
        var message = new HttpRequestMessage(HttpMethod.Get, path);

        return SendAsync(message, IsReceiptPath(path), ct, notFoundNeedsNodeCode: true);
    }

    /// <inheritdoc />
    /// <remarks>STUB: refuses until the node exposes a consumer-reachable treasury balance read.</remarks>
    public Task<Result<ProjectTreasuryBalance>> GetBalanceAsync(
        string chainType,
        string projectReference,
        string? assetId,
        CancellationToken ct = default)
    {
        _ = chainType;
        _ = assetId;
        _ = ct;

        _logger.LogWarning(
            "Project treasury balance read refused for {ProjectReference}: no consumer-reachable node route.",
            projectReference);

        return Task.FromResult(Result<ProjectTreasuryBalance>.Failure(
            "The project's treasury balance cannot be read from the node with a tenant credential. "
            + UnblockedBy));
    }

    /// <inheritdoc />
    /// <remarks>STUB: refuses until the node exposes a tenant-scoped holdings read.</remarks>
    public Task<Result<AvatarHolding>> GetHoldingAsync(
        Guid avatarId,
        string assetId,
        CancellationToken ct = default)
    {
        _ = assetId;
        _ = ct;

        _logger.LogWarning(
            "Custodial holdings read refused for avatar {AvatarId}: no consumer-reachable node route.",
            avatarId);

        return Task.FromResult(Result<AvatarHolding>.Failure(
            "This account's holdings cannot be read from the node with a tenant credential. "
            + UnblockedBy));
    }

    /// <summary>Sends an exchange call, refusing any path outside the exchange route family.</summary>
    private async Task<Result<ArdaNovaExchangeReceipt>> SendAsync(
        HttpRequestMessage message,
        bool pathAllowed,
        CancellationToken ct,
        bool notFoundNeedsNodeCode = false)
    {
        if (!pathAllowed)
        {
            message.Dispose();
            return Result<ArdaNovaExchangeReceipt>.Forbidden(
                "The AZOA value credential is restricted to exchange endpoints.");
        }

        var wire = await AzoaNodeClient.SendAsync<AzoaExchangeWireReceipt>(_http, _logger, message, ct, notFoundNeedsNodeCode);
        if (wire.IsFailure)
            return MapFailure(wire);

        return Result<ArdaNovaExchangeReceipt>.Success(FromWire(wire.Value!));
    }

    /// <summary>Projects an Application command onto the node's exchange body.</summary>
    private AzoaExchangeWireRequest ToWire(ArdaNovaExchangeCommand command) =>
        new()
        {
            ChainType = string.IsNullOrWhiteSpace(command.ChainType) ? _settings.ChainType : command.ChainType,
            Direction = command.IsBuy ? "Buy" : "Sell",
            ProjectReference = command.ProjectReference,
            ProjectAssetId = command.ProjectAssetId,
            PlatformAmount = command.PlatformAmountBaseUnits.ToString(CultureInfo.InvariantCulture),
            ProjectUnitAmount = command.ProjectUnitAmountBaseUnits.ToString(CultureInfo.InvariantCulture),
            ReservePartition = command.ReservePartition,
            ReleaseNotBefore = command.ReleaseNotBefore,
            Purpose = command.Purpose,
            PreviewOnly = command.PreviewOnly,
            DeclaredRate = new AzoaDeclaredRateWire
            {
                CurrencyCode = "USD",
                MajorUnitsPerWholeUnit = command.Rate.UsdMajorUnitsPerWholeProjectUnit,
                AssetDecimals = command.Rate.ProjectUnitDecimals,
                CurrencyMinorUnitDigits = 2,
                DeclaredAt = command.Rate.DeclaredAt,
                Source = command.Rate.Source,
                Version = command.Rate.Version,
            },
        };

    /// <summary>Projects the node's receipt onto the Application-owned shape.</summary>
    private static ArdaNovaExchangeReceipt FromWire(AzoaExchangeWireReceipt wire) =>
        new(
            ReceiptReference: wire.ReceiptReference ?? string.Empty,
            OperationId: wire.OperationId,
            State: wire.State ?? "AwaitingReconciliation",
            IsTerminal: wire.IsTerminal,
            RequiresReconciliation: wire.RequiresReconciliation,
            Replayed: wire.Replayed,
            AmountSpentBaseUnits: wire.AmountSpent,
            AmountReceivedBaseUnits: wire.AmountReceived,
            ProjectCreditBaseUnits: wire.ProjectCreditAmount,
            SettlementReference: wire.SettlementReference,
            HoldReference: wire.HoldReference,
            HoldReleasesAt: wire.HoldReleasesAt,
            FailureCode: wire.FailureCode);

    /// <summary>Re-wraps a transport failure, keeping the result type (including KYC_FORBIDDEN).</summary>
    private static Result<ArdaNovaExchangeReceipt> MapFailure(Result<AzoaExchangeWireReceipt> source)
    {
        var error = source.Error ?? "AZOA exchange failed.";
        return source.Type switch
        {
            ResultType.NotFound => Result<ArdaNovaExchangeReceipt>.NotFound(error),
            ResultType.Forbidden => Result<ArdaNovaExchangeReceipt>.Forbidden(error),
            ResultType.Unauthorized => Result<ArdaNovaExchangeReceipt>.Unauthorized(error),
            ResultType.Conflict => Result<ArdaNovaExchangeReceipt>.Conflict(error),
            ResultType.ValidationError => Result<ArdaNovaExchangeReceipt>.ValidationError(error),
            ResultType.BadRequest => Result<ArdaNovaExchangeReceipt>.BadRequest(error),
            _ => Result<ArdaNovaExchangeReceipt>.Failure(error),
        };
    }

    /// <summary>POST target: the exchange route for exactly one avatar.</summary>
    private static bool IsExchangePath(string? path)
        => AzoaNodePathPolicy.IsSafeApiPath(path)
            && path!.StartsWith(ExchangePrefix, StringComparison.Ordinal)
            && Guid.TryParseExact(path[ExchangePrefix.Length..], "D", out _);

    /// <summary>GET target: the receipt route for exactly one idempotency key.</summary>
    private static bool IsReceiptPath(string? path)
        => AzoaNodePathPolicy.IsSafeApiPath(path)
            && path!.StartsWith(ReceiptPrefix, StringComparison.Ordinal)
            && IsAcceptableIdempotencyKey(path[ReceiptPrefix.Length..]);

    /// <summary>The node's own key alphabet, applied before the key can reach a URL.</summary>
    private static bool IsAcceptableIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        var trimmed = key.Trim();
        if (trimmed.Length > MaxIdempotencyKeyLength)
            return false;

        foreach (var character in trimmed)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_' or ':' or '.'))
                return false;
        }

        return true;
    }
}

/// <summary>Wire shape of the node's declared rate. Exact decimal strings, never floats.</summary>
internal sealed class AzoaDeclaredRateWire
{
    [JsonPropertyName("currencyCode")] public string? CurrencyCode { get; set; }
    [JsonPropertyName("majorUnitsPerWholeUnit")] public string? MajorUnitsPerWholeUnit { get; set; }
    [JsonPropertyName("minorUnitsPerBaseUnit")] public string? MinorUnitsPerBaseUnit { get; set; }
    [JsonPropertyName("assetDecimals")] public int? AssetDecimals { get; set; }
    [JsonPropertyName("currencyMinorUnitDigits")] public int CurrencyMinorUnitDigits { get; set; } = 2;
    [JsonPropertyName("declaredAt")] public DateTimeOffset? DeclaredAt { get; set; }
    [JsonPropertyName("source")] public string? Source { get; set; }
    [JsonPropertyName("version")] public string? Version { get; set; }
}

/// <summary>Wire shape of one exchange. The recipient is the route value, never a field here.</summary>
internal sealed class AzoaExchangeWireRequest
{
    [JsonPropertyName("chainType")] public string ChainType { get; set; } = string.Empty;
    [JsonPropertyName("direction")] public string Direction { get; set; } = "Buy";
    [JsonPropertyName("projectReference")] public string ProjectReference { get; set; } = string.Empty;
    [JsonPropertyName("projectAssetId")] public string ProjectAssetId { get; set; } = string.Empty;

    /// <summary>Base units of the platform unit, as a whole decimal string.</summary>
    [JsonPropertyName("platformAmount")] public string PlatformAmount { get; set; } = string.Empty;

    /// <summary>Base units of the project unit, as a whole decimal string.</summary>
    [JsonPropertyName("projectUnitAmount")] public string ProjectUnitAmount { get; set; } = string.Empty;

    [JsonPropertyName("reservePartition")] public string ReservePartition { get; set; } = string.Empty;
    [JsonPropertyName("declaredRate")] public AzoaDeclaredRateWire? DeclaredRate { get; set; }
    [JsonPropertyName("releaseNotBefore")] public DateTimeOffset? ReleaseNotBefore { get; set; }
    [JsonPropertyName("holdReference")] public string? HoldReference { get; set; }
    [JsonPropertyName("purpose")] public string? Purpose { get; set; }
    [JsonPropertyName("previewOnly")] public bool PreviewOnly { get; set; }
}

/// <summary>Wire shape of the node's exchange receipt.</summary>
internal sealed class AzoaExchangeWireReceipt
{
    [JsonPropertyName("receiptReference")] public string? ReceiptReference { get; set; }
    [JsonPropertyName("operationId")] public Guid? OperationId { get; set; }
    [JsonPropertyName("state")] public string? State { get; set; }
    [JsonPropertyName("isTerminal")] public bool IsTerminal { get; set; }
    [JsonPropertyName("requiresReconciliation")] public bool RequiresReconciliation { get; set; }
    [JsonPropertyName("replayed")] public bool Replayed { get; set; }
    [JsonPropertyName("amountSpent")] public string? AmountSpent { get; set; }
    [JsonPropertyName("amountSpentAssetId")] public string? AmountSpentAssetId { get; set; }
    [JsonPropertyName("amountReceived")] public string? AmountReceived { get; set; }
    [JsonPropertyName("amountReceivedAssetId")] public string? AmountReceivedAssetId { get; set; }
    [JsonPropertyName("projectCreditAmount")] public string? ProjectCreditAmount { get; set; }
    [JsonPropertyName("settlementReference")] public string? SettlementReference { get; set; }
    [JsonPropertyName("holdReference")] public string? HoldReference { get; set; }
    [JsonPropertyName("holdReleasesAt")] public DateTimeOffset? HoldReleasesAt { get; set; }
    [JsonPropertyName("failureCode")] public string? FailureCode { get; set; }
}

/// <summary>
/// Registration for the exchange adapter, kept here so wiring it costs exactly one call in
/// <c>Infrastructure.DependencyInjection</c> - the same arrangement AZOA uses for its own
/// per-subsystem registration files.
/// </summary>
public static class AzoaExchangeClientRegistration
{
    /// <summary>
    /// Registers the exchange transport and the three ports it satisfies. Redirect following
    /// stays off so a scoped key cannot cross origins.
    /// </summary>
    public static IServiceCollection AddAzoaExchangeClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string? environmentName = null)
    {
        var settings = configuration.GetSection(AzoaSettings.SectionName).Get<AzoaSettings>()
            ?? new AzoaSettings();
        var isProduction = string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase);
        var valueApiKey = AzoaCredentialSelection.ResolveValueKey(settings, isProduction);

        services.AddHttpClient<AzoaExchangeClient>(client =>
        {
            if (Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseUri))
                client.BaseAddress = baseUri;
            if (!string.IsNullOrWhiteSpace(valueApiKey))
                client.DefaultRequestHeaders.Add("X-Api-Key", valueApiKey);
            client.Timeout = TimeSpan.FromSeconds(
                settings.TimeoutSeconds > 0 ? settings.TimeoutSeconds : 30);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
        });

        // One instance behind all three ports: every exchange caller shares one adapter.
        services.AddScoped<IAzoaExchangeNode>(provider => provider.GetRequiredService<AzoaExchangeClient>());
        services.AddScoped<IAzoaProjectTreasuryReadModel>(provider => provider.GetRequiredService<AzoaExchangeClient>());
        services.AddScoped<IAzoaHoldingsReadModel>(provider => provider.GetRequiredService<AzoaExchangeClient>());

        return services;
    }
}
