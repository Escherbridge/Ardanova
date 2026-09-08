namespace ArdaNova.Infrastructure.Azoa;

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// The ONE adapter every ArdaNova caller uses for the node's HOLD surface (D14 / W3.4).
/// </summary>
/// <remarks>
/// A hold is how "these units are yours" stops being a promise in ArdaNova's database and
/// becomes a commitment the node's reserve ledger has actually drawn. Opening a hold is the
/// point at which the node — not this database — decides whether a named partition can still
/// afford the award, which is why every worker award goes through here rather than through a
/// local supply counter.
/// <para>
/// It carries the scoped value credential to <c>/api/holds</c> only, the same route-family
/// restriction the other typed clients apply (see this directory's AGENTS.md), and it never
/// lets a node message reach a caller verbatim — <see cref="AzoaNodeClient.SendAsync"/> maps
/// status codes to stable public text first.
/// </para>
/// <para>
/// Amounts are opaque whole base-unit strings. ArdaNova has already decided them at the
/// asset's scale; the node derives no economics of its own (AZOA contract sections 1 and 3).
/// </para>
/// </remarks>
public sealed class AzoaHoldClient : IAzoaHoldGateway
{
    private const string HoldsPath = "/api/holds";
    private const string HoldsPrefix = HoldsPath + "/";
    private const int MaximumHoldIdLength = 128;

    private readonly HttpClient _http;
    private readonly ILogger<AzoaHoldClient> _logger;
    private readonly AzoaSettings _settings;

    public AzoaHoldClient(
        HttpClient http,
        ILogger<AzoaHoldClient> logger,
        IOptions<AzoaSettings> settings)
    {
        _http = http;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public Task<Result<AzoaHold>> OpenAsync(
        AzoaHoldOpenRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            return Refused("A hold cannot be opened without a request.");

        if (DescribeUnusableReference(request.Reference) is { } badReference)
            return Refused(badReference);

        if (string.IsNullOrWhiteSpace(request.AssetId))
            return Refused("A hold needs the stake it is held in.");

        if (!IsWholeBaseUnitCount(request.AmountBaseUnits))
            return Refused("A hold amount must be a positive whole base-unit count.");

        if (string.IsNullOrWhiteSpace(request.ReservePartition))
            return Refused("A hold must name the allowance it is drawn from.");

        if (request.BeneficiaryAvatarId == Guid.Empty)
            return Refused("A hold needs the person the units are held for.");

        var chain = ResolveChain(request.Reference.ChainType);
        if (!IsUsableSegment(chain))
            return Refused("A hold must name a rail this deployment can address.");

        var message = new HttpRequestMessage(HttpMethod.Post, HoldsPath)
        {
            Content = JsonContent.Create(new AzoaHoldOpenWire
            {
                Chain = chain,
                HoldId = request.Reference.HoldId,
                AssetId = request.AssetId,
                Amount = request.AmountBaseUnits,
                ReservePartition = request.ReservePartition,
                BeneficiaryUserId = request.BeneficiaryAvatarId,
                ReleaseNotBefore = request.ReleaseNotBefore,
                AutoRelease = request.AutoRelease,
                Purpose = request.Purpose,
            }),
        };

        return SendAsync(message, IsHoldCollectionPath(HoldsPath), ct);
    }

    /// <inheritdoc />
    public Task<Result<AzoaHold>> ReleaseIntoHoldAsync(
        AzoaHoldChainRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            return Refused("A hold cannot be continued without a request.");

        if (DescribeUnusableReference(request.Reference) is { } badReference)
            return Refused(badReference);

        if (!IsUsableHoldId(request.NextHoldId))
            return Refused("Continuing a hold needs a usable id for the hold that continues it.");

        if (request.NextBeneficiaryAvatarId == Guid.Empty)
            return Refused("Continuing a hold needs the person the units are held for next.");

        if (DescribeUnusableChain(request.Reference) is { } badChain)
            return Refused(badChain);

        var path = ActionPath(request.Reference, "release-into-hold");
        var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new AzoaHoldChainWire
            {
                NextHoldId = request.NextHoldId,
                NextBeneficiaryUserId = request.NextBeneficiaryAvatarId,
                ReleaseNotBefore = request.ReleaseNotBefore,
                AutoRelease = request.AutoRelease,
                Purpose = request.Purpose,
            }),
        };

        return SendAsync(message, IsHoldActionPath(path), ct);
    }

    /// <inheritdoc />
    public Task<Result<AzoaHold>> RefundAsync(
        AzoaHoldReference reference,
        string? reason,
        CancellationToken ct = default)
    {
        if (DescribeUnusableReference(reference) is { } badReference)
            return Refused(badReference);

        if (DescribeUnusableChain(reference) is { } badChain)
            return Refused(badChain);

        var path = ActionPath(reference, "refund");
        var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new AzoaHoldSettlementWire { Reason = reason }),
        };

        return SendAsync(message, IsHoldActionPath(path), ct);
    }

    /// <inheritdoc />
    public Task<Result<AzoaHold>> GetAsync(
        AzoaHoldReference reference,
        CancellationToken ct = default)
    {
        if (DescribeUnusableReference(reference) is { } badReference)
            return Refused(badReference);

        if (DescribeUnusableChain(reference) is { } badChain)
            return Refused(badChain);

        var path = HoldsPrefix + ResolveChain(reference.ChainType) + "/" + reference.HoldId.Trim();
        var message = new HttpRequestMessage(HttpMethod.Get, path);

        return SendAsync(message, IsHoldReadPath(path), ct);
    }

    /// <summary>Sends a hold call, refusing any path outside the hold route family.</summary>
    private async Task<Result<AzoaHold>> SendAsync(
        HttpRequestMessage message,
        bool pathAllowed,
        CancellationToken ct)
    {
        if (!pathAllowed)
        {
            message.Dispose();
            return Result<AzoaHold>.Forbidden(
                "The AZOA value credential is restricted to hold endpoints.");
        }

        var wire = await AzoaNodeClient.SendAsync<AzoaHoldWire>(_http, _logger, message, ct);
        if (wire.IsFailure)
            return MapFailure(wire);

        return Result<AzoaHold>.Success(FromWire(wire.Value!));
    }

    private string ResolveChain(string? chainType)
        => string.IsNullOrWhiteSpace(chainType) ? _settings.ChainType : chainType.Trim();

    private string ActionPath(AzoaHoldReference reference, string action)
        => HoldsPrefix + ResolveChain(reference.ChainType) + "/" + reference.HoldId.Trim() + "/" + action;

    /// <summary>Null when the reference's rail is a usable path segment; the refusal otherwise.</summary>
    private string? DescribeUnusableChain(AzoaHoldReference reference)
        => IsUsableSegment(ResolveChain(reference.ChainType))
            ? null
            : "A hold must name a rail this deployment can address.";

    /// <summary>Null when the reference can address a hold; the person-facing refusal otherwise.</summary>
    private static string? DescribeUnusableReference(AzoaHoldReference? reference)
    {
        if (reference is null)
            return "A hold needs a reference.";

        return IsUsableHoldId(reference.HoldId)
            ? null
            : "A hold reference must carry an id of at most "
                + MaximumHoldIdLength
                + " letters, digits, dots, dashes or underscores.";
    }

    /// <summary>The id alphabet, applied before an id can reach a URL segment.</summary>
    private static bool IsUsableHoldId(string? holdId) => IsUsableSegment(holdId);

    /// <summary>
    /// One path segment, unescaped. Escaping would satisfy the transport's own safety check
    /// while still handing the node a different id than the one this database recorded, so a
    /// value that is not already a clean segment is refused instead of encoded.
    /// </summary>
    private static bool IsUsableSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (trimmed.Length > MaximumHoldIdLength)
            return false;

        foreach (var character in trimmed)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_' or '.'))
                return false;
        }

        return true;
    }

    private static bool IsWholeBaseUnitCount(string? amount)
    {
        if (string.IsNullOrWhiteSpace(amount))
            return false;

        var trimmed = amount.Trim();
        return trimmed.All(char.IsAsciiDigit) && trimmed.Any(character => character != '0');
    }

    /// <summary>POST target: the hold collection itself.</summary>
    private static bool IsHoldCollectionPath(string? path)
        => AzoaNodePathPolicy.IsSafeApiPath(path)
            && string.Equals(path, HoldsPath, StringComparison.Ordinal);

    /// <summary>POST target: exactly one hold's release, refund or continuation.</summary>
    private static bool IsHoldActionPath(string? path)
        => AzoaNodePathPolicy.IsSafeApiPath(path)
            && path!.StartsWith(HoldsPrefix, StringComparison.Ordinal)
            && path.Split('/') is { Length: 6 } segments
            && segments[^1] is "release" or "refund" or "release-into-hold";

    /// <summary>GET target: exactly one hold.</summary>
    private static bool IsHoldReadPath(string? path)
        => AzoaNodePathPolicy.IsSafeApiPath(path)
            && path!.StartsWith(HoldsPrefix, StringComparison.Ordinal)
            && path.Split('/').Length == 5;

    /// <summary>Projects the node's hold view onto the Application-owned shape.</summary>
    private static AzoaHold FromWire(AzoaHoldWire wire)
        => new(
            HoldId: wire.HoldId ?? string.Empty,
            ChainType: wire.Chain ?? string.Empty,
            AssetId: wire.AssetId ?? string.Empty,
            AmountBaseUnits: wire.Amount ?? string.Empty,
            ReservePartition: wire.ReservePartition ?? string.Empty,
            BeneficiaryReference: wire.BeneficiaryUserId,
            State: wire.State ?? AzoaHoldStates.Unknown,
            ReleaseNotBefore: wire.ReleaseNotBefore,
            AutoRelease: wire.AutoRelease,
            ParentHoldId: wire.ParentHoldId,
            ChildHoldId: wire.ChildHoldId);

    /// <summary>Re-wraps a transport failure, keeping the result type (including KYC_FORBIDDEN).</summary>
    private static Result<AzoaHold> MapFailure(Result<AzoaHoldWire> source)
    {
        var error = source.Error ?? "The AZOA hold call failed.";
        return source.Type switch
        {
            ResultType.NotFound => Result<AzoaHold>.NotFound(error),
            ResultType.Forbidden => Result<AzoaHold>.Forbidden(error),
            ResultType.Unauthorized => Result<AzoaHold>.Unauthorized(error),
            ResultType.Conflict => Result<AzoaHold>.Conflict(error),
            ResultType.ValidationError => Result<AzoaHold>.ValidationError(error),
            ResultType.BadRequest => Result<AzoaHold>.BadRequest(error),
            _ => Result<AzoaHold>.Failure(error),
        };
    }

    private static Task<Result<AzoaHold>> Refused(string reason)
        => Task.FromResult(Result<AzoaHold>.ValidationError(reason));
}

/// <summary>Wire body of an open call. The acting tenant is the credential's, never a field.</summary>
internal sealed class AzoaHoldOpenWire
{
    [JsonPropertyName("chain")] public string Chain { get; set; } = string.Empty;
    [JsonPropertyName("holdId")] public string HoldId { get; set; } = string.Empty;
    [JsonPropertyName("assetId")] public string AssetId { get; set; } = string.Empty;

    /// <summary>Base units of the held asset, as a whole decimal string.</summary>
    [JsonPropertyName("amount")] public string Amount { get; set; } = string.Empty;

    [JsonPropertyName("reservePartition")] public string ReservePartition { get; set; } = string.Empty;
    [JsonPropertyName("beneficiaryUserId")] public Guid BeneficiaryUserId { get; set; }
    [JsonPropertyName("releaseNotBefore")] public DateTimeOffset? ReleaseNotBefore { get; set; }
    [JsonPropertyName("autoRelease")] public bool AutoRelease { get; set; }
    [JsonPropertyName("purpose")] public string? Purpose { get; set; }
}

/// <summary>Wire body of a release-into-hold call.</summary>
internal sealed class AzoaHoldChainWire
{
    [JsonPropertyName("nextHoldId")] public string NextHoldId { get; set; } = string.Empty;
    [JsonPropertyName("nextBeneficiaryUserId")] public Guid NextBeneficiaryUserId { get; set; }
    [JsonPropertyName("releaseNotBefore")] public DateTimeOffset? ReleaseNotBefore { get; set; }
    [JsonPropertyName("autoRelease")] public bool AutoRelease { get; set; }
    [JsonPropertyName("purpose")] public string? Purpose { get; set; }
}

/// <summary>Wire body of a release or refund call.</summary>
internal sealed class AzoaHoldSettlementWire
{
    [JsonPropertyName("reason")] public string? Reason { get; set; }
}

/// <summary>Wire shape of one hold as the node reports it. No address, no rail reference.</summary>
internal sealed class AzoaHoldWire
{
    [JsonPropertyName("holdId")] public string? HoldId { get; set; }
    [JsonPropertyName("chain")] public string? Chain { get; set; }
    [JsonPropertyName("assetId")] public string? AssetId { get; set; }
    [JsonPropertyName("amount")] public string? Amount { get; set; }
    [JsonPropertyName("reservePartition")] public string? ReservePartition { get; set; }
    [JsonPropertyName("beneficiaryUserId")] public string? BeneficiaryUserId { get; set; }
    [JsonPropertyName("state")] public string? State { get; set; }
    [JsonPropertyName("releaseNotBefore")] public DateTimeOffset? ReleaseNotBefore { get; set; }
    [JsonPropertyName("autoRelease")] public bool AutoRelease { get; set; }
    [JsonPropertyName("parentHoldId")] public string? ParentHoldId { get; set; }
    [JsonPropertyName("childHoldId")] public string? ChildHoldId { get; set; }
}

/// <summary>
/// Registration for the hold adapter, kept here so wiring it costs exactly one call in
/// <c>Infrastructure.DependencyInjection</c> — the same arrangement the exchange adapter uses.
/// </summary>
public static class AzoaHoldClientRegistration
{
    /// <summary>
    /// Registers the hold transport and the port it satisfies. Redirect following stays off so
    /// a scoped key cannot cross origins. Registering nothing is a supported state: every
    /// caller resolves the port as a collection and refuses, by name, when it is empty.
    /// </summary>
    public static IServiceCollection AddAzoaHoldClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string? environmentName = null)
    {
        var settings = configuration.GetSection(AzoaSettings.SectionName).Get<AzoaSettings>()
            ?? new AzoaSettings();
        var isProduction = string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase);
        var valueApiKey = AzoaCredentialSelection.ResolveValueKey(settings, isProduction);

        services.AddHttpClient<AzoaHoldClient>(client =>
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

        services.AddScoped<IAzoaHoldGateway>(provider => provider.GetRequiredService<AzoaHoldClient>());

        return services;
    }
}
