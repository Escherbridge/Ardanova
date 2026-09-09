namespace ArdaNova.Infrastructure.Azoa;

using System.Globalization;
using System.Net.Http.Json;
using System.Numerics;
using System.Text.Json.Serialization;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// The node-side half of funding setup: where a project's money lands, and how much of its
/// stake each side of the project is allowed to be issued.
/// </summary>
/// <remarks>
/// <para>This is the only implementation of <see cref="IAzoaProjectTokenNodeGateway"/>. Until it
/// existed <c>ProjectTokenService.ProvisionAsync</c> refused every project by name, so nothing
/// on the platform could be funded at all - the port was declared and no adapter satisfied it.</para>
/// <para><b>Two credentials, deliberately.</b> The two legs are different authorities on the node
/// and are therefore carried by different keys:</para>
/// <list type="bullet">
/// <item>Treasury resolve (<c>POST /api/projects/treasury/resolve</c>) is tenant onboarding -
/// the node asks for <c>tenant:provision</c> - so it goes over this class's OWN HttpClient,
/// carrying <c>Azoa:CustodyApiKey</c>. That is the key ArdaNova's tenant credential already
/// carries the scope on (see this directory's AGENTS.md).</item>
/// <item>Partition declaration (<c>POST /api/reserve/{assetId}/partitions</c>) is the same
/// authority that created the stake - the node asks for <c>token:create:sign</c> - so it goes
/// over <see cref="IAzoaNodeClient"/>, carrying <c>Azoa:ValueApiKey</c>.</item>
/// </list>
/// <para>Routing both legs over one key would have widened whichever key it was: the value key
/// would gain tenant onboarding, or the custody key would gain the ability to carve budgets on
/// the shared reserve.</para>
/// <para><b>Both legs check the answer, not just the status.</b> A treasury whose echoed project
/// reference is not the one asked for, or which comes back with no address, is refused rather
/// than recorded - <c>ProjectTokenService</c> writes whatever this returns into
/// <c>config.treasuryAddress</c> and thereafter calls the project funded. Likewise a partition
/// declaration whose echoed budget disagrees with what was declared is a failure, because the
/// caller's next act is to stamp <c>partitionsDeclaredAt</c> and open the project for money.</para>
/// </remarks>
public sealed class AzoaProjectTokenNodeGateway : IAzoaProjectTokenNodeGateway
{
    /// <summary>Tenant-scoped treasury resolve. See the class remarks for why it is the custody key.</summary>
    private const string TreasuryResolvePath = "/api/projects/treasury/resolve";

    /// <summary>Longest project reference the node will be asked to scope by.</summary>
    private const int MaximumProjectReferenceLength = 200;

    /// <summary>Most partitions one declaration may name; mirrors the node's own limit.</summary>
    private const int MaximumPartitionsPerDeclaration = 64;

    /// <summary>Longest partition name the node accepts.</summary>
    private const int MaximumPartitionNameLength = 64;

    private readonly HttpClient _http;
    private readonly IAzoaNodeClient _valueNode;
    private readonly ILogger<AzoaProjectTokenNodeGateway> _logger;

    public AzoaProjectTokenNodeGateway(
        HttpClient http,
        IAzoaNodeClient valueNode,
        ILogger<AzoaProjectTokenNodeGateway> logger)
    {
        _http = http;
        _valueNode = valueNode;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AzoaProjectTreasury>> ResolveTreasuryAsync(
        AzoaProjectTreasuryRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            return Result<AzoaProjectTreasury>.ValidationError(
                "A project needs to say which chain and which project before it can be given an account.");

        var chainType = request.ChainType?.Trim() ?? string.Empty;
        var projectReference = request.ProjectReference?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(chainType))
            return Result<AzoaProjectTreasury>.ValidationError(
                "A project treasury must name the rail it lives on.");

        if (string.IsNullOrEmpty(projectReference) || projectReference.Length > MaximumProjectReferenceLength)
            return Result<AzoaProjectTreasury>.ValidationError(
                $"A project treasury must name the project, in at most {MaximumProjectReferenceLength} characters.");

        // There is no tenant field: the node reads the tenant off the credential this client
        // carries. A tenant on the body would be a way to reach another tenant's treasuries.
        var message = new HttpRequestMessage(HttpMethod.Post, TreasuryResolvePath)
        {
            Content = JsonContent.Create(new ProjectTreasuryResolveWire
            {
                ChainType = chainType,
                ProjectReference = projectReference,
            }),
        };

        var wire = await SendCustodyAsync<ProjectTreasuryWire>(
            message, AzoaNodeRoutePolicy.IsTreasuryResolvePath(message.RequestUri?.OriginalString), ct);

        if (wire.IsFailure)
            return Result<AzoaProjectTreasury>.Failure(wire.Error!);

        var account = wire.Value!;
        var address = account.TreasuryAddress?.Trim() ?? string.Empty;

        // An account with no address is somewhere for funds to land in name only. Recorded, it
        // would take the project all the way to "accepting funding" with nowhere for the funding
        // to go, so it is a failure here rather than an empty string in the database.
        if (string.IsNullOrEmpty(address))
        {
            _logger.LogError(
                "The node resolved a treasury for project {ProjectReference} with no address.",
                projectReference);
            return Result<AzoaProjectTreasury>.Failure(
                "The project could not be given an account for funds to land in.");
        }

        // The reference round-trips through the node untouched but for a trim, so a different one
        // coming back means this address belongs to a different project. Writing it would point
        // one project's funding at another project's account.
        var echoedReference = account.ProjectReference?.Trim() ?? string.Empty;
        if (!string.Equals(echoedReference, projectReference, StringComparison.Ordinal))
        {
            _logger.LogError(
                "The node answered a treasury resolve for {ProjectReference} with {EchoedReference}.",
                projectReference,
                echoedReference);
            return Result<AzoaProjectTreasury>.Failure(
                "The project could not be given an account for funds to land in.");
        }

        return Result<AzoaProjectTreasury>.Success(new AzoaProjectTreasury(
            string.IsNullOrWhiteSpace(account.ChainType) ? chainType : account.ChainType.Trim(),
            echoedReference,
            address,
            string.IsNullOrWhiteSpace(account.Status) ? "Unknown" : account.Status.Trim()));
    }

    /// <inheritdoc />
    public async Task<Result<AzoaReservePartitions>> DeclareReservePartitionsAsync(
        AzoaReservePartitionRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            return Result<AzoaReservePartitions>.ValidationError(
                "An allowance declaration needs the stake it is drawn from.");

        var chainType = request.ChainType?.Trim() ?? string.Empty;
        var assetId = request.AssetId?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(chainType))
            return Result<AzoaReservePartitions>.ValidationError(
                "An allowance declaration must name the rail the stake lives on.");

        var path = $"/api/reserve/{assetId}/partitions";
        if (!AzoaNodeRoutePolicy.IsReservePartitionsPath(path))
            return Result<AzoaReservePartitions>.ValidationError(
                "An allowance declaration must name a stake this deployment can address.");

        var declared = ReadDeclaredBudgets(request.DeclaredBaseUnitsByPartition, out var budgetError);
        if (declared is null)
            return Result<AzoaReservePartitions>.ValidationError(budgetError);

        // The rail travels in the BODY rather than as a '?chain=' query: the shared path policy
        // refuses a query string outright, and the node reads either.
        var body = new ReservePartitionDeclarationWire
        {
            Chain = chainType,
            Partitions = declared.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.ToString(CultureInfo.InvariantCulture),
                StringComparer.Ordinal),
        };

        var wire = await _valueNode.PostAsync<ReservePartitionsWire>(path, body, ct);
        if (wire.IsFailure)
            return Result<AzoaReservePartitions>.Failure(wire.Error!);

        var view = wire.Value!;
        var recorded = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (name, budget) in declared)
        {
            var echoed = view.Partitions?.FirstOrDefault(partition =>
                string.Equals(partition.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase));

            // The node SETS a budget rather than adding to it, so what comes back is what a draw
            // will be measured against. A partition that is absent, or that stands at a different
            // number, means the allowances this project is about to be opened on are not the ones
            // it declared - and the caller's very next act is to call the project fundable.
            if (echoed is null
                || !BigInteger.TryParse(
                    echoed.Declared, NumberStyles.None, CultureInfo.InvariantCulture, out var echoedBudget)
                || echoedBudget != budget)
            {
                _logger.LogError(
                    "The node recorded allowance '{Partition}' on stake {AssetId} as {Recorded}, not {Declared}.",
                    name,
                    assetId,
                    echoed?.Declared ?? "(absent)",
                    budget.ToString(CultureInfo.InvariantCulture));

                return Result<AzoaReservePartitions>.Failure(
                    "The worker and sponsor allowances could not be registered.");
            }

            recorded[name] = echoedBudget.ToString(CultureInfo.InvariantCulture);
        }

        return Result<AzoaReservePartitions>.Success(new AzoaReservePartitions(
            string.IsNullOrWhiteSpace(view.AssetId) ? assetId : view.AssetId.Trim(),
            recorded));
    }

    /// <summary>Sends a custody-key call, refusing any path outside the treasury route family.</summary>
    private Task<Result<T>> SendCustodyAsync<T>(
        HttpRequestMessage message, bool pathAllowed, CancellationToken ct)
    {
        if (!pathAllowed)
        {
            message.Dispose();
            return Task.FromResult(Result<T>.Forbidden(
                "The AZOA custody credential is restricted to tenant and project-treasury endpoints."));
        }

        return AzoaNodeClient.SendAsync<T>(_http, _logger, message, ct);
    }

    /// <summary>
    /// Reads the declared allowances, or says what is wrong with them. The rules mirror the
    /// node's own (<c>NumberStyles.None</c>: no sign, no separators, no whitespace), so a budget
    /// it would refuse never costs a round trip - and a NEGATIVE allowance, which would be a
    /// budget larger than the supply once it wrapped, cannot be spelled at all.
    /// </summary>
    private static Dictionary<string, BigInteger>? ReadDeclaredBudgets(
        IReadOnlyDictionary<string, string>? declared, out string error)
    {
        error = string.Empty;

        if (declared is null || declared.Count == 0)
        {
            error = "An allowance declaration must name at least one allowance and its budget.";
            return null;
        }

        if (declared.Count > MaximumPartitionsPerDeclaration)
        {
            error = $"An allowance declaration may name at most {MaximumPartitionsPerDeclaration} allowances.";
            return null;
        }

        var budgets = new Dictionary<string, BigInteger>(StringComparer.Ordinal);
        foreach (var (rawName, rawBudget) in declared)
        {
            var name = rawName?.Trim().ToLowerInvariant() ?? string.Empty;

            if (string.IsNullOrEmpty(name) || name.Length > MaximumPartitionNameLength)
            {
                error = $"An allowance name must be 1 to {MaximumPartitionNameLength} characters.";
                return null;
            }

            // The node treats allowance names case-insensitively, so two spellings of one name
            // would silently keep whichever was iterated last.
            if (budgets.ContainsKey(name))
            {
                error = $"This declaration names the allowance '{name}' more than once.";
                return null;
            }

            if (!BigInteger.TryParse(
                    rawBudget, NumberStyles.None, CultureInfo.InvariantCulture, out var budget))
            {
                error = $"The allowance '{name}' needs its budget as a whole, non-negative count.";
                return null;
            }

            budgets[name] = budget;
        }

        return budgets;
    }
}

/// <summary>What a tenant posts to get one of its projects an account. No tenant field, on purpose.</summary>
internal sealed class ProjectTreasuryResolveWire
{
    [JsonPropertyName("chainType")] public string ChainType { get; set; } = string.Empty;
    [JsonPropertyName("projectReference")] public string ProjectReference { get; set; } = string.Empty;
}

/// <summary>Wire shape of the account a project's funding lands in, as the node reports it.</summary>
internal sealed class ProjectTreasuryWire
{
    [JsonPropertyName("chainType")] public string? ChainType { get; set; }
    [JsonPropertyName("projectReference")] public string? ProjectReference { get; set; }
    [JsonPropertyName("treasuryAddress")] public string? TreasuryAddress { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
}

/// <summary>Body of an allowance declaration. The rail is a locator; it carries no authority.</summary>
internal sealed class ReservePartitionDeclarationWire
{
    [JsonPropertyName("partitions")] public Dictionary<string, string> Partitions { get; set; } = new();
    [JsonPropertyName("chain")] public string? Chain { get; set; }
}

/// <summary>Wire shape of where one stake's allowances stand, as the node reports it.</summary>
internal sealed class ReservePartitionsWire
{
    [JsonPropertyName("assetId")] public string? AssetId { get; set; }
    [JsonPropertyName("chain")] public string? Chain { get; set; }
    [JsonPropertyName("network")] public string? Network { get; set; }
    [JsonPropertyName("unitName")] public string? UnitName { get; set; }
    [JsonPropertyName("decimals")] public int Decimals { get; set; }
    [JsonPropertyName("totalSupply")] public string? TotalSupply { get; set; }
    [JsonPropertyName("version")] public long Version { get; set; }
    [JsonPropertyName("partitions")] public List<ReservePartitionWire>? Partitions { get; set; }
}

/// <summary>One allowance's budget and what it has already committed, as exact decimal strings.</summary>
internal sealed class ReservePartitionWire
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("declared")] public string? Declared { get; set; }
    [JsonPropertyName("issued")] public string? Issued { get; set; }
    [JsonPropertyName("held")] public string? Held { get; set; }
    [JsonPropertyName("returned")] public string? Returned { get; set; }
    [JsonPropertyName("available")] public string? Available { get; set; }
}

/// <summary>
/// Registration for the project funding gateway, kept beside it so wiring costs exactly one call
/// in <c>Infrastructure.DependencyInjection</c> - the same arrangement the hold and cash-out
/// adapters use.
/// </summary>
public static class AzoaProjectTokenNodeGatewayRegistration
{
    /// <summary>
    /// Registers the treasury transport and the <see cref="IAzoaProjectTokenNodeGateway"/> binding
    /// it satisfies. The binding is added ONLY when BOTH credentials this gateway needs actually
    /// resolve - the custody key for the treasury leg and the value key for the allowance leg -
    /// because a gateway holding one of the two would refuse halfway through provisioning and
    /// leave a project with a stake and no allowances. Registering nothing is a supported state:
    /// <c>ProjectTokenService</c> resolves the port as a collection and refuses funding setup by
    /// name rather than half-provisioning.
    /// </summary>
    public static IServiceCollection AddAzoaProjectTokenGateway(
        this IServiceCollection services,
        IConfiguration configuration,
        string? environmentName = null)
    {
        var settings = configuration.GetSection(AzoaSettings.SectionName).Get<AzoaSettings>()
            ?? new AzoaSettings();
        var isProduction = string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase);
        var custodyApiKey = AzoaCredentialSelection.ResolveCustodyKey(settings, isProduction);
        var valueApiKey = AzoaCredentialSelection.ResolveValueKey(settings, isProduction);
        var transportConfigured =
            Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _)
            && !string.IsNullOrWhiteSpace(custodyApiKey)
            && !string.IsNullOrWhiteSpace(valueApiKey);

        services.AddHttpClient<AzoaProjectTokenNodeGateway>(client =>
        {
            if (Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseUri))
                client.BaseAddress = baseUri;
            if (!string.IsNullOrWhiteSpace(custodyApiKey))
                client.DefaultRequestHeaders.Add("X-Api-Key", custodyApiKey);
            client.Timeout = TimeSpan.FromSeconds(
                settings.TimeoutSeconds > 0 ? settings.TimeoutSeconds : 30);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            // A redirect off the node is a redirect of where a project's money lands.
            AllowAutoRedirect = false,
        });

        if (!transportConfigured)
            return services;

        services.AddScoped<IAzoaProjectTokenNodeGateway>(
            provider => provider.GetRequiredService<AzoaProjectTokenNodeGateway>());
        return services;
    }
}
