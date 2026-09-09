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
/// The ONE adapter that puts a membership credential in the member's own hands, over the node's
/// issuer-facing door (<c>POST /api/credential/issue/{recipientAvatarId}</c>).
/// </summary>
/// <remarks>
/// <para>Until this existed <c>CredentialUtilityService._issuanceNode</c> was null, so every
/// credential ArdaNova granted stayed "Verification pending" - correctly, because nothing had
/// been issued. This adapter is what makes the difference between a row in ArdaNova's own
/// database and a credential the member holds and ArdaNova can later withdraw.</para>
/// <para><b>Its own credential, and why.</b> The node's issue door asks two independent things of
/// the caller: the <c>dapp:manager</c> ROLE on the key's owner, and the <c>nft:mint</c> SCOPE on
/// the key. Neither the custody key (tenant onboarding) nor the value key (stake creation and
/// allowances) carries that pair, so this client carries <c>Azoa:IssuanceApiKey</c> and no other
/// route family can be reached with it.</para>
/// <para><b>What is checked before a proof is recorded.</b> Its caller writes whatever comes back
/// into the credential's proof columns and stamps <c>CredentialProofOrigin.IssuanceNode</c>, which
/// is the single thing that makes a member's card read "independently verifiable". So this adapter
/// refuses a reply that does not actually evidence an issuance to THAT member: no issuance id, a
/// holder who is not the recipient, or a row the node did not make soulbound and non-transferable.
/// A refusal leaves the credential pending, which is the truth.</para>
/// <para><b>The issuer is deliberately not checked here.</b> The node writes
/// <c>issuer_user_id</c> in a second statement AFTER the row it echoes back is projected
/// (<c>SurrealNftStore.UpsertUserNFTAsync</c>), so a successful issuance still reports an empty
/// issuer on the wire. Asserting it would refuse every genuine issuance - and the node already
/// fails the whole mint, by name, when that second write does not land.</para>
/// <para><b>Idempotency, honestly.</b> The stable key is sent as <c>Idempotency-Key</c>, but the
/// node's replay ledger currently covers the allocation door only: the issue route takes no
/// request context and does not dedupe. What stops a second credential today is the caller -
/// <c>CredentialUtilityService</c> issues once per credential id and only retries a credential
/// still recorded as pending. The header is sent so the guarantee becomes real the moment the
/// node's ledger covers this route, not as a claim that it already does.</para>
/// </remarks>
public sealed class AzoaCredentialIssuanceClient : IAzoaCredentialIssuanceNode
{
    private const string IssuePrefix = "/api/credential/issue/";
    private const int MaximumIdempotencyKeyLength = 128;
    private const int MaximumAttributeLength = 256;

    private readonly HttpClient _http;
    private readonly ILogger<AzoaCredentialIssuanceClient> _logger;
    private readonly AzoaSettings _settings;

    public AzoaCredentialIssuanceClient(
        HttpClient http,
        ILogger<AzoaCredentialIssuanceClient> logger,
        IOptions<AzoaSettings> settings)
    {
        _http = http;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public async Task<Result<MembershipCredentialIssuance>> IssueAsync(
        Guid recipientAvatarId,
        MembershipCredentialIssueRequest request,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        if (recipientAvatarId == Guid.Empty)
            return Result<MembershipCredentialIssuance>.ValidationError(
                "A credential needs the member it is issued to.");

        if (request is null)
            return Result<MembershipCredentialIssuance>.ValidationError(
                "A credential needs what it is a credential of.");

        if (string.IsNullOrWhiteSpace(request.CredentialId))
            return Result<MembershipCredentialIssuance>.ValidationError(
                "A credential needs its own identity before it can be issued.");

        if (string.IsNullOrWhiteSpace(request.MetadataUri))
            return Result<MembershipCredentialIssuance>.ValidationError(
                "A credential cannot be issued without the description that says what it is.");

        if (!IsAcceptableIdempotencyKey(idempotencyKey))
            return Result<MembershipCredentialIssuance>.ValidationError(
                "A stable idempotency key of 1-" + MaximumIdempotencyKeyLength
                + " plain characters is required so a repeated grant cannot issue twice.");

        var chainType = string.IsNullOrWhiteSpace(_settings.ChainType) ? "Algorand" : _settings.ChainType.Trim();
        var path = $"{IssuePrefix}{recipientAvatarId:D}";

        var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            // The recipient is the ROUTE value and the issuer is the credential this client
            // carries; neither is a body field, so no body can redirect a credential or nominate
            // a different revocation authority.
            Content = JsonContent.Create(new CredentialIssueWire
            {
                ChainType = chainType,
                MetadataUri = request.MetadataUri.Trim(),
                Name = DescribeName(request),
                Description = DescribeCredential(request),
                Attributes = DescribeAttributes(request),
                // Restated here as well as on the node: a transferable "credential" would leave
                // the issuer's withdrawal authority pointing at a holder the record no longer names.
                IsSoulbound = true,
                IsTransferable = false,
            }),
        };
        message.Headers.Add("Idempotency-Key", idempotencyKey.Trim());

        var wire = await SendAsync<CredentialIssuanceWire>(
            message, AzoaNodeRoutePolicy.IsCredentialIssuePath(message.RequestUri?.OriginalString), ct);

        if (wire.IsFailure)
            return Result<MembershipCredentialIssuance>.Failure(wire.Error!);

        return Interpret(wire.Value!, recipientAvatarId, request.CredentialId);
    }

    /// <summary>
    /// Turns the node's row into an issuance, or refuses it. See the class remarks: everything
    /// below decides whether a member's card is allowed to say the credential is real.
    /// </summary>
    private Result<MembershipCredentialIssuance> Interpret(
        CredentialIssuanceWire row, Guid recipientAvatarId, string credentialId)
    {
        Result<MembershipCredentialIssuance> Refuse(string reason)
        {
            _logger.LogError(
                "The node's reply to issuing credential {CredentialId} was not evidence of an issuance: {Reason}",
                credentialId,
                reason);

            return Result<MembershipCredentialIssuance>.Conflict(
                "This credential could not be confirmed as issued, so it stays unverified.");
        }

        if (row.Id == Guid.Empty)
            return Refuse("it carries no issuance identity");

        if (row.UserId != recipientAvatarId)
            return Refuse($"it is held by {row.UserId}, not the member it was issued to");

        if (!row.IsSoulbound || row.IsTransferable)
            return Refuse("the issued row is transferable, so it could leave the member's hands");

        var proofReference = string.IsNullOrWhiteSpace(row.TokenId) ? null : row.TokenId.Trim();

        return Result<MembershipCredentialIssuance>.Success(new MembershipCredentialIssuance(
            row.Id.ToString("D"),
            proofReference,
            string.IsNullOrWhiteSpace(row.MetadataUri) ? null : row.MetadataUri.Trim(),
            row.MintedDate == default
                ? DateTimeOffset.UtcNow
                : new DateTimeOffset(DateTime.SpecifyKind(row.MintedDate, DateTimeKind.Utc))));
    }

    /// <summary>Sends an issuance call, refusing any path outside the issue route family.</summary>
    private Task<Result<T>> SendAsync<T>(HttpRequestMessage message, bool pathAllowed, CancellationToken ct)
    {
        if (!pathAllowed)
        {
            message.Dispose();
            return Task.FromResult(Result<T>.Forbidden(
                "The AZOA issuance credential is restricted to the credential-issue endpoint."));
        }

        return AzoaNodeClient.SendAsync<T>(_http, _logger, message, ct);
    }

    /// <summary>What the credential is called, in the member's own vocabulary.</summary>
    private static string DescribeName(MembershipCredentialIssueRequest request)
        => string.IsNullOrWhiteSpace(request.Tier)
            ? "Membership"
            : $"{request.Tier.Trim()} membership";

    /// <summary>One sentence saying what the credential is a membership of.</summary>
    private static string DescribeCredential(MembershipCredentialIssueRequest request)
        => string.IsNullOrWhiteSpace(request.GrantedVia)
            ? "Membership recorded by ArdaNova."
            : $"Membership recorded by ArdaNova, granted through {request.GrantedVia.Trim()}.";

    /// <summary>
    /// The trace back to ArdaNova's own record. Every value is bounded and non-null: the node
    /// persists these verbatim and an unbounded one would be a way to write arbitrary length
    /// into a shared row.
    /// </summary>
    private static Dictionary<string, string> DescribeAttributes(MembershipCredentialIssueRequest request)
    {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["credentialId"] = Bounded(request.CredentialId),
            ["scope"] = Bounded(request.Scope),
            ["scopeId"] = Bounded(request.ScopeId),
            ["grantedVia"] = Bounded(request.GrantedVia),
        };

        if (!string.IsNullOrWhiteSpace(request.Tier))
            attributes["tier"] = Bounded(request.Tier);

        return attributes;
    }

    private static string Bounded(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        return trimmed.Length > MaximumAttributeLength
            ? trimmed[..MaximumAttributeLength]
            : trimmed;
    }

    /// <summary>The key alphabet a header may carry, applied before it can reach a request.</summary>
    private static bool IsAcceptableIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        var trimmed = key.Trim();
        if (trimmed.Length > MaximumIdempotencyKeyLength)
            return false;

        foreach (var character in trimmed)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_' or ':' or '.'))
                return false;
        }

        return true;
    }
}

/// <summary>
/// Wire shape of what the node is asked to issue - the fields of its <c>UserNFTMintModel</c> a
/// credential actually uses. The recipient and the issuer are deliberately absent: the node takes
/// the recipient from the route and the issuer from the presented credential.
/// </summary>
internal sealed class CredentialIssueWire
{
    [JsonPropertyName("chainType")] public string ChainType { get; set; } = string.Empty;
    [JsonPropertyName("metadataURI")] public string MetadataUri { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("attributes")] public Dictionary<string, string> Attributes { get; set; } = new();
    [JsonPropertyName("isSoulbound")] public bool IsSoulbound { get; set; }
    [JsonPropertyName("isTransferable")] public bool IsTransferable { get; set; }
}

/// <summary>Wire shape of the issued row, as the node reports it back.</summary>
internal sealed class CredentialIssuanceWire
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("userId")] public Guid UserId { get; set; }
    [JsonPropertyName("tokenId")] public string? TokenId { get; set; }
    [JsonPropertyName("metadataURI")] public string? MetadataUri { get; set; }
    [JsonPropertyName("isSoulbound")] public bool IsSoulbound { get; set; }
    [JsonPropertyName("isTransferable")] public bool IsTransferable { get; set; }
    [JsonPropertyName("mintedDate")] public DateTime MintedDate { get; set; }
}

/// <summary>
/// Registration for the credential issuance adapter, kept beside it so wiring costs exactly one
/// call in <c>Infrastructure.DependencyInjection</c>.
/// </summary>
public static class AzoaCredentialIssuanceRegistration
{
    /// <summary>Configuration key holding the <c>dapp:manager</c> + <c>nft:mint</c> node credential.</summary>
    public const string IssuanceApiKeyPath = AzoaSettings.SectionName + ":IssuanceApiKey";

    /// <summary>
    /// Registers the issuance transport and the <see cref="IAzoaCredentialIssuanceNode"/> binding
    /// it satisfies. The binding is added ONLY when a base URL and a NON-BLANK issuance key both
    /// resolve. Blank resolves to "not configured" explicitly rather than by falling through to
    /// another key: a credential this client could reach a different route family with would be a
    /// wider authority than issuing, and a blank environment variable is the easiest shape a
    /// deployment produces. Registering nothing is a supported state - <c>CredentialUtilityService</c>
    /// then records the grant and leaves the card reading "Verification pending", which is true.
    /// </summary>
    public static IServiceCollection AddAzoaCredentialIssuance(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(AzoaSettings.SectionName).Get<AzoaSettings>()
            ?? new AzoaSettings();
        var issuanceApiKey = configuration[IssuanceApiKeyPath]?.Trim() ?? string.Empty;
        var transportConfigured =
            Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _)
            && !string.IsNullOrEmpty(issuanceApiKey);

        services.AddHttpClient<AzoaCredentialIssuanceClient>(client =>
        {
            if (Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseUri))
                client.BaseAddress = baseUri;
            if (!string.IsNullOrEmpty(issuanceApiKey))
                client.DefaultRequestHeaders.Add("X-Api-Key", issuanceApiKey);
            client.Timeout = TimeSpan.FromSeconds(
                settings.TimeoutSeconds > 0 ? settings.TimeoutSeconds : 30);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
        });

        if (!transportConfigured)
            return services;

        services.AddScoped<IAzoaCredentialIssuanceNode>(
            provider => provider.GetRequiredService<AzoaCredentialIssuanceClient>());
        return services;
    }
}
