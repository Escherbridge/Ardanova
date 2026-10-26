namespace ArdaNova.Infrastructure.Azoa;

using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// The ONE adapter every ArdaNova caller uses for the node's HOLD surface (D14 / W3.4),
/// speaking the node's keyed command door <c>/api/hold-commands</c>.
/// </summary>
/// <remarks>
/// Every award hold is reserve-funded, so each command is a LOCAL node transition that
/// completes synchronously. Route shapes, the derived hold-record id and the auto-release
/// limit: see this directory's AGENTS.md §Hold commands.
/// </remarks>
public sealed class AzoaHoldClient : IAzoaHoldGateway
{
    private const string CommandsPath = "/api/hold-commands";
    private const string ReserveAssetsPath = CommandsPath + "/reserve-assets";
    private const string LiabilitiesPrefix = CommandsPath + "/liabilities/";
    private const int MaximumHoldIdLength = 128;
    private const int HoldRecordIdLength = 64;
    private const string ReserveFunding = "Reserve";

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
    public async Task<Result<AzoaHold>> OpenAsync(
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

        if (request.AutoRelease)
            return Refused(AutoReleaseRefusal);

        var chain = ResolveChain(request.Reference.ChainType);
        if (!IsUsableSegment(chain))
            return Refused("A hold must name a rail this deployment can address.");

        if (ResolveTenantUser() is not { } tenantUser)
            return Result<AzoaHold>.Failure(TenantUnsetError);

        var assets = await ListReserveAssetsAsync(ct);
        if (assets.IsFailure)
            return Rewrap<List<AzoaHoldReserveAssetWire>>(assets);

        var asset = assets.Value!
            .Select(candidate => candidate.Asset)
            .FirstOrDefault(candidate => candidate is not null
                && string.Equals(candidate.ChainType, chain, StringComparison.Ordinal)
                && string.Equals(candidate.AssetId, request.AssetId.Trim(), StringComparison.Ordinal));
        if (asset is null)
        {
            return Result<AzoaHold>.NotFound(
                "This stake is not registered with the shared node's reserve under this platform's account, "
                + "so nothing was set aside.");
        }

        var holdId = request.Reference.HoldId.Trim();
        var intent = new AzoaHoldIntentWire
        {
            RequestKey = "open:" + holdId,
            Kind = AzoaHoldCommandKinds.Open,
            HoldId = holdId,
            ActorUserId = tenantUser,
            OwnerUserId = tenantUser,
            Asset = asset,
            Definition = new AzoaHoldDefinitionWire
            {
                Asset = asset,
                QuantityBaseUnits = request.AmountBaseUnits.Trim(),
                FundingSource = ReserveFunding,
                BeneficiaryUserId = request.BeneficiaryAvatarId,
                ReservePartition = request.ReservePartition.Trim(),
                ReleaseNotBefore = request.ReleaseNotBefore,
                AutoRelease = false,
                Purpose = request.Purpose,
            },
        };

        var view = await StartAsync(intent, ct);
        if (view.IsFailure)
            return Rewrap<AzoaHoldCommandViewWire>(view);

        return FromCommand(view.Value!, request.ReservePartition.Trim(), request.ReleaseNotBefore, null);
    }

    /// <inheritdoc />
    public async Task<Result<AzoaHold>> ReleaseIntoHoldAsync(
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

        if (request.AutoRelease)
            return Refused(AutoReleaseRefusal);

        if (DescribeUnusableChain(request.Reference) is { } badChain)
            return Refused(badChain);

        var held = await FindLiabilityAsync(request.Reference, ct);
        if (held.IsFailure)
            return Rewrap<AzoaHoldLiabilityWire>(held);

        var liability = held.Value!;
        var holdId = request.Reference.HoldId.Trim();
        var nextHoldId = request.NextHoldId.Trim();
        var intent = new AzoaHoldIntentWire
        {
            RequestKey = "release-into-hold:" + holdId + ":" + nextHoldId,
            Kind = AzoaHoldCommandKinds.ReleaseIntoHold,
            HoldId = holdId,
            ActorUserId = liability.OwnerUserId,
            OwnerUserId = liability.OwnerUserId,
            Asset = liability.Asset,
            NextHoldId = nextHoldId,
            NextBeneficiaryUserId = request.NextBeneficiaryAvatarId,
            NextReleaseNotBefore = request.ReleaseNotBefore,
            NextAutoRelease = false,
            Reason = request.Purpose,
        };

        var view = await StartAsync(intent, ct);
        if (view.IsFailure)
            return Rewrap<AzoaHoldCommandViewWire>(view);

        return FromCommand(view.Value!, liability.ReservePartition ?? string.Empty, liability.ReleaseNotBefore, nextHoldId);
    }

    /// <inheritdoc />
    public async Task<Result<AzoaHold>> RefundAsync(
        AzoaHoldReference reference,
        string? reason,
        CancellationToken ct = default)
    {
        if (DescribeUnusableReference(reference) is { } badReference)
            return Refused(badReference);

        if (DescribeUnusableChain(reference) is { } badChain)
            return Refused(badChain);

        var held = await FindLiabilityAsync(reference, ct);
        if (held.IsFailure)
            return Rewrap<AzoaHoldLiabilityWire>(held);

        var liability = held.Value!;
        var holdId = reference.HoldId.Trim();
        var intent = new AzoaHoldIntentWire
        {
            RequestKey = "refund:" + holdId,
            Kind = AzoaHoldCommandKinds.Refund,
            HoldId = holdId,
            ActorUserId = liability.OwnerUserId,
            OwnerUserId = liability.OwnerUserId,
            Asset = liability.Asset,
            Reason = reason,
        };

        var view = await StartAsync(intent, ct);
        if (view.IsFailure)
            return Rewrap<AzoaHoldCommandViewWire>(view);

        return FromCommand(view.Value!, liability.ReservePartition ?? string.Empty, liability.ReleaseNotBefore, null);
    }

    /// <inheritdoc />
    public async Task<Result<AzoaHold>> GetAsync(
        AzoaHoldReference reference,
        CancellationToken ct = default)
    {
        if (DescribeUnusableReference(reference) is { } badReference)
            return Refused(badReference);

        if (DescribeUnusableChain(reference) is { } badChain)
            return Refused(badChain);

        var held = await FindLiabilityAsync(reference, ct);
        return held.IsFailure
            ? Rewrap<AzoaHoldLiabilityWire>(held)
            : Result<AzoaHold>.Success(FromLiability(held.Value!));
    }

    /// <summary>
    /// The live liability a reference names. The node addresses a hold by a record id derived
    /// from its custody identity, so each network this tenant's reserve exposes on the rail is
    /// one candidate address.
    /// </summary>
    private async Task<Result<AzoaHoldLiabilityWire>> FindLiabilityAsync(
        AzoaHoldReference reference,
        CancellationToken ct)
    {
        if (ResolveTenantUser() is not { } tenantUser)
            return Result<AzoaHoldLiabilityWire>.Failure(TenantUnsetError);

        var assets = await ListReserveAssetsAsync(ct);
        if (assets.IsFailure)
            return Rewrap<List<AzoaHoldReserveAssetWire>, AzoaHoldLiabilityWire>(assets);

        var chain = ResolveChain(reference.ChainType);
        var custodies = assets.Value!
            .Select(candidate => candidate.Asset)
            .Where(candidate => candidate is not null
                && string.Equals(candidate.ChainType, chain, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(candidate.ProtocolIdentity))
            .Select(candidate => (candidate!.Network, candidate.ProtocolIdentity))
            .Distinct()
            .ToList();

        foreach (var (network, protocolIdentity) in custodies)
        {
            var recordId = AzoaHoldRecordIdentity.For(
                chain, network, protocolIdentity, custodyTenantId: null, tenantUser, reference.HoldId.Trim());
            if (recordId is null)
                continue;

            var path = LiabilitiesPrefix + recordId;
            var read = await SendAsync<AzoaHoldLiabilityWire>(
                new HttpRequestMessage(HttpMethod.Get, path), IsLiabilityReadPath(path), ct);
            if (read.IsSuccess && read.Value!.Asset is not null)
                return read;

            if (read.IsFailure && read.Type != ResultType.NotFound)
                return read;
        }

        return Result<AzoaHoldLiabilityWire>.NotFound("The shared node holds nothing under this reference.");
    }

    private Task<Result<List<AzoaHoldReserveAssetWire>>> ListReserveAssetsAsync(CancellationToken ct)
        => SendAsync<List<AzoaHoldReserveAssetWire>>(
            new HttpRequestMessage(HttpMethod.Get, ReserveAssetsPath), IsReserveAssetsPath(ReserveAssetsPath), ct);

    private Task<Result<AzoaHoldCommandViewWire>> StartAsync(AzoaHoldIntentWire intent, CancellationToken ct)
        => SendAsync<AzoaHoldCommandViewWire>(
            new HttpRequestMessage(HttpMethod.Post, CommandsPath)
            {
                Content = JsonContent.Create(new AzoaHoldStartWire { Intent = intent }),
            },
            IsCommandStartPath(CommandsPath),
            ct);

    /// <summary>Sends a hold call, refusing any path outside the hold route family.</summary>
    private async Task<Result<T>> SendAsync<T>(HttpRequestMessage message, bool pathAllowed, CancellationToken ct)
    {
        if (!pathAllowed)
        {
            message.Dispose();
            return Result<T>.Forbidden("The AZOA value credential is restricted to hold endpoints.");
        }

        return await AzoaNodeClient.SendAsync<T>(_http, _logger, message, ct);
    }

    private Guid? ResolveTenantUser()
        => Guid.TryParse(_settings.TenantId, out var tenant) && tenant != Guid.Empty ? tenant : null;

    private string ResolveChain(string? chainType)
        => string.IsNullOrWhiteSpace(chainType) ? _settings.ChainType : chainType.Trim();

    /// <summary>Null when the reference's rail is a usable path segment; the refusal otherwise.</summary>
    private string? DescribeUnusableChain(AzoaHoldReference reference)
        => IsUsableSegment(ResolveChain(reference.ChainType))
            ? null
            : "A hold must name a rail this deployment can address.";

    private const string TenantUnsetError =
        "The shared node's hold surface needs this platform's tenant account (Azoa:TenantId), which is not configured.";

    private const string AutoReleaseRefusal =
        "The shared node cannot release a hold on its own without a release mandate, so this hold must ask to be released.";

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

    /// <summary>The id alphabet, applied before an id can reach a request key or record id.</summary>
    private static bool IsUsableHoldId(string? holdId) => IsUsableSegment(holdId);

    /// <summary>One clean segment: a value outside the alphabet is refused, never encoded.</summary>
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

    /// <summary>POST target: the keyed command door itself.</summary>
    private static bool IsCommandStartPath(string? path)
        => AzoaNodePathPolicy.IsSafeApiPath(path) && string.Equals(path, CommandsPath, StringComparison.Ordinal);

    /// <summary>GET target: the reserve assets this tenant may open holds against.</summary>
    private static bool IsReserveAssetsPath(string? path)
        => AzoaNodePathPolicy.IsSafeApiPath(path) && string.Equals(path, ReserveAssetsPath, StringComparison.Ordinal);

    /// <summary>GET target: exactly one liability, addressed by its lowercase-hex record id.</summary>
    private static bool IsLiabilityReadPath(string? path)
        => AzoaNodePathPolicy.IsSafeApiPath(path)
            && path!.StartsWith(LiabilitiesPrefix, StringComparison.Ordinal)
            && path[LiabilitiesPrefix.Length..] is { Length: HoldRecordIdLength } recordId
            && recordId.All(character => char.IsAsciiDigit(character) || character is >= 'a' and <= 'f');

    /// <summary>Projects a completed command's view onto the Application-owned hold shape.</summary>
    private static Result<AzoaHold> FromCommand(
        AzoaHoldCommandViewWire view,
        string reservePartition,
        DateTimeOffset? releaseNotBefore,
        string? childHoldId)
    {
        if (view.State is AzoaHoldCommandStates.Cancelled or AzoaHoldCommandStates.FailedNoEffect)
            return Result<AzoaHold>.BadRequest("AZOA node declined the request; nothing was moved.");

        var holdState = view.State == AzoaHoldCommandStates.Completed
            ? view.Kind switch
            {
                AzoaHoldCommandKinds.Open => AzoaHoldStates.Held,
                AzoaHoldCommandKinds.Refund => AzoaHoldStates.Refunded,
                AzoaHoldCommandKinds.ReleaseIntoHold => AzoaHoldStates.ReleasedIntoHold,
                _ => AzoaHoldStates.Unknown,
            }
            : AzoaHoldStates.Pending;

        return Result<AzoaHold>.Success(new AzoaHold(
            HoldId: view.HoldId ?? string.Empty,
            ChainType: view.Asset?.ChainType ?? string.Empty,
            AssetId: view.Asset?.AssetId ?? string.Empty,
            AmountBaseUnits: view.QuantityBaseUnits ?? string.Empty,
            ReservePartition: reservePartition,
            BeneficiaryReference: view.BeneficiaryUserId?.ToString(),
            State: holdState,
            ReleaseNotBefore: releaseNotBefore,
            AutoRelease: false,
            ParentHoldId: null,
            ChildHoldId: holdState == AzoaHoldStates.ReleasedIntoHold ? childHoldId : null));
    }

    /// <summary>Projects the node's liability view onto the Application-owned hold shape.</summary>
    private static AzoaHold FromLiability(AzoaHoldLiabilityWire wire)
        => new(
            HoldId: wire.HoldId ?? string.Empty,
            ChainType: wire.Asset?.ChainType ?? string.Empty,
            AssetId: wire.Asset?.AssetId ?? string.Empty,
            AmountBaseUnits: wire.QuantityBaseUnits ?? string.Empty,
            ReservePartition: wire.ReservePartition ?? string.Empty,
            BeneficiaryReference: wire.BeneficiaryUserId?.ToString(),
            State: string.IsNullOrWhiteSpace(wire.State) ? AzoaHoldStates.Unknown : wire.State,
            ReleaseNotBefore: wire.ReleaseNotBefore,
            AutoRelease: wire.AutoRelease,
            ParentHoldId: null,
            ChildHoldId: null);

    private static Result<AzoaHold> Rewrap<TSource>(Result<TSource> source)
        => Rewrap<TSource, AzoaHold>(source);

    /// <summary>Re-wraps a transport failure, keeping the result type (including KYC_FORBIDDEN).</summary>
    private static Result<TOut> Rewrap<TSource, TOut>(Result<TSource> source)
    {
        var error = source.Error ?? "The AZOA hold call failed.";
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

    private static Result<AzoaHold> Refused(string reason) => Result<AzoaHold>.ValidationError(reason);
}

/// <summary>Command kinds the hold door admits for reserve-funded holds.</summary>
internal static class AzoaHoldCommandKinds
{
    public const string Open = "Open";
    public const string Refund = "Refund";
    public const string ReleaseIntoHold = "ReleaseIntoHold";
}

/// <summary>Command (not hold) states the door reports; 200 for the last three, 202 otherwise.</summary>
internal static class AzoaHoldCommandStates
{
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string FailedNoEffect = "FailedNoEffect";
}

/// <summary>
/// The node's hold-record id: SHA-256 over canonical JSON of the custody identity, mirroring
/// <c>HoldCommandIdentity.Hold</c> (domain <c>hold-custody-v1</c>). See AGENTS.md §Hold commands.
/// </summary>
internal static class AzoaHoldRecordIdentity
{
    private const string Domain = "hold-custody-v1";

    /// <summary>The record id, or null when the network word is not one the node defines.</summary>
    public static string? For(
        string chainType,
        string? network,
        string protocolIdentity,
        Guid? custodyTenantId,
        Guid ownerUserId,
        string holdId)
    {
        // The node hashes ChainNetwork with default serializer options, i.e. as its ordinal.
        int? ordinal = network switch
        {
            "Devnet" => 0,
            "Testnet" => 1,
            "Mainnet" => 2,
            _ => null,
        };
        if (ordinal is null)
            return null;

        var element = JsonSerializer.SerializeToElement(new
        {
            Domain,
            ChainType = chainType,
            Network = ordinal.Value,
            ProtocolIdentity = protocolIdentity,
            CustodyTenantId = custodyTenantId,
            OwnerUserId = ownerUserId,
            HoldId = holdId,
        });

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
            WriteCanonical(writer, element);

        return Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()));
    }

    /// <summary>Ordinal key sort, array order and raw number tokens preserved (node CanonicalJson).</summary>
    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in value.EnumerateObject().OrderBy(item => item.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in value.EnumerateArray())
                    WriteCanonical(writer, item);
                writer.WriteEndArray();
                break;
            case JsonValueKind.Number:
                writer.WriteRawValue(value.GetRawText());
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(value.GetString());
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }
}

/// <summary>Body of <c>POST /api/hold-commands</c> (node <c>HoldStartRequest</c>). Reserve holds name no wallet.</summary>
internal sealed class AzoaHoldStartWire
{
    [JsonPropertyName("intent")] public AzoaHoldIntentWire Intent { get; set; } = new();
    [JsonPropertyName("senderWalletId")] public Guid? SenderWalletId { get; set; }
    [JsonPropertyName("recipientWalletId")] public Guid? RecipientWalletId { get; set; }
}

/// <summary>
/// Node <c>HoldCommandIntent</c>. Tenant and custody tenant stay null: a plain tenant key carries
/// no acting-tenant claim, and the node requires the intent to match the captured credential.
/// </summary>
internal sealed class AzoaHoldIntentWire
{
    [JsonPropertyName("version")] public int Version { get; set; } = 1;
    [JsonPropertyName("requestKey")] public string RequestKey { get; set; } = string.Empty;
    [JsonPropertyName("kind")] public string Kind { get; set; } = string.Empty;
    [JsonPropertyName("holdId")] public string HoldId { get; set; } = string.Empty;
    [JsonPropertyName("actorUserId")] public Guid ActorUserId { get; set; }
    [JsonPropertyName("ownerUserId")] public Guid OwnerUserId { get; set; }
    [JsonPropertyName("tenantId")] public Guid? TenantId { get; set; }
    [JsonPropertyName("custodyTenantId")] public Guid? CustodyTenantId { get; set; }
    [JsonPropertyName("asset")] public AzoaHoldAssetWire? Asset { get; set; }
    [JsonPropertyName("definition")] public AzoaHoldDefinitionWire? Definition { get; set; }
    [JsonPropertyName("nextHoldId")] public string? NextHoldId { get; set; }
    [JsonPropertyName("nextBeneficiaryUserId")] public Guid? NextBeneficiaryUserId { get; set; }
    [JsonPropertyName("nextReleaseNotBefore")] public DateTimeOffset? NextReleaseNotBefore { get; set; }
    [JsonPropertyName("nextAutoRelease")] public bool NextAutoRelease { get; set; }
    [JsonPropertyName("reason")] public string? Reason { get; set; }
}

/// <summary>Node <c>HoldAssetIdentity</c>, echoed verbatim from the node's own reads.</summary>
internal sealed record AzoaHoldAssetWire
{
    [JsonPropertyName("chainType")] public string ChainType { get; init; } = string.Empty;

    /// <summary><c>ChainNetwork</c> as the node's string-enum converter writes it.</summary>
    [JsonPropertyName("network")] public string Network { get; init; } = string.Empty;

    [JsonPropertyName("protocolIdentity")] public string ProtocolIdentity { get; init; } = string.Empty;
    [JsonPropertyName("assetId")] public string AssetId { get; init; } = string.Empty;
    [JsonPropertyName("holdingAddress")] public string HoldingAddress { get; init; } = string.Empty;
}

/// <summary>Node <c>HoldDefinition</c> for a reserve-funded opening.</summary>
internal sealed class AzoaHoldDefinitionWire
{
    [JsonPropertyName("asset")] public AzoaHoldAssetWire? Asset { get; set; }

    /// <summary>Base units of the held asset, as a whole decimal string.</summary>
    [JsonPropertyName("quantityBaseUnits")] public string QuantityBaseUnits { get; set; } = string.Empty;

    [JsonPropertyName("fundingSource")] public string FundingSource { get; set; } = string.Empty;
    [JsonPropertyName("beneficiaryUserId")] public Guid BeneficiaryUserId { get; set; }
    [JsonPropertyName("funderUserId")] public Guid? FunderUserId { get; set; }
    [JsonPropertyName("reservePartition")] public string? ReservePartition { get; set; }
    [JsonPropertyName("releaseNotBefore")] public DateTimeOffset? ReleaseNotBefore { get; set; }
    [JsonPropertyName("autoRelease")] public bool AutoRelease { get; set; }
    [JsonPropertyName("purpose")] public string? Purpose { get; set; }
}

/// <summary>Node <c>HoldCommandView</c>: command progress plus the resulting hold identity.</summary>
internal sealed class AzoaHoldCommandViewWire
{
    [JsonPropertyName("operationId")] public string? OperationId { get; set; }
    [JsonPropertyName("kind")] public string? Kind { get; set; }
    [JsonPropertyName("state")] public string? State { get; set; }
    [JsonPropertyName("holdRecordId")] public string? HoldRecordId { get; set; }
    [JsonPropertyName("holdId")] public string? HoldId { get; set; }
    [JsonPropertyName("asset")] public AzoaHoldAssetWire? Asset { get; set; }
    [JsonPropertyName("quantityBaseUnits")] public string? QuantityBaseUnits { get; set; }
    [JsonPropertyName("beneficiaryUserId")] public Guid? BeneficiaryUserId { get; set; }
    [JsonPropertyName("nextHoldRecordId")] public string? NextHoldRecordId { get; set; }
}

/// <summary>Node <c>HoldLiabilityView</c>: one hold as the node currently records it.</summary>
internal sealed class AzoaHoldLiabilityWire
{
    [JsonPropertyName("holdRecordId")] public string? HoldRecordId { get; set; }
    [JsonPropertyName("holdId")] public string? HoldId { get; set; }
    [JsonPropertyName("asset")] public AzoaHoldAssetWire? Asset { get; set; }
    [JsonPropertyName("quantityBaseUnits")] public string? QuantityBaseUnits { get; set; }
    [JsonPropertyName("state")] public string? State { get; set; }
    [JsonPropertyName("ownerUserId")] public Guid OwnerUserId { get; set; }
    [JsonPropertyName("beneficiaryUserId")] public Guid? BeneficiaryUserId { get; set; }
    [JsonPropertyName("custodyTenantId")] public Guid? CustodyTenantId { get; set; }
    [JsonPropertyName("fundingSource")] public string? FundingSource { get; set; }
    [JsonPropertyName("reservePartition")] public string? ReservePartition { get; set; }
    [JsonPropertyName("releaseNotBefore")] public DateTimeOffset? ReleaseNotBefore { get; set; }
    [JsonPropertyName("autoRelease")] public bool AutoRelease { get; set; }
}

/// <summary>Node <c>HoldReserveAssetView</c>: an asset this caller's draw authority admits.</summary>
internal sealed class AzoaHoldReserveAssetWire
{
    [JsonPropertyName("asset")] public AzoaHoldAssetWire? Asset { get; set; }
    [JsonPropertyName("unitName")] public string? UnitName { get; set; }
    [JsonPropertyName("decimals")] public int Decimals { get; set; }
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
