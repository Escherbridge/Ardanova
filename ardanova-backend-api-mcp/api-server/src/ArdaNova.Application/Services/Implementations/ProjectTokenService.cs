namespace ArdaNova.Application.Services.Implementations;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.Policies;
using ArdaNova.Domain.ValueObjects;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Manages project token configurations, allocations (by holder class: CONTRIBUTOR, INVESTOR, FOUNDER),
/// distribution, revocation, founder token burning, and investor trust protection.
/// </summary>
public class ProjectTokenService : IProjectTokenService, IProjectTokenProvisioning
{
    /// <summary>Reserve-ledger partition worker stake is drawn from on the node.</summary>
    public const string ContributorPartitionName = "contributor";

    /// <summary>Reserve-ledger partition sponsor stake is drawn from on the node.</summary>
    public const string InvestorPartitionName = "investor";

    /// <summary>
    /// The issuing scale every project stake is created at. It is the PLATFORM's, not the
    /// owner's: <see cref="IAlgorandService.CreateFungibleASAAsync"/> carries no decimals
    /// parameter and both providers behind it mint at 6, so declaring anything else here
    /// would put ArdaNova's unit count and the chain's a million-fold apart. Owner-chosen
    /// precision (Q-X3) unblocks when that surface takes a scale.
    /// </summary>
    public const int PlatformAssetScale = 6;

    private const string DefaultChainType = "Algorand";
    private const int MaximumAssetNameLength = 32;
    private const int MaximumUnitNameLength = 8;
    private const int MinimumDisclosureLength = 20;
    private const int MaximumDisclosureLength = 2000;

    private const string ProvisioningUnwiredError =
        "Project funding setup is unavailable in this deployment: the shared-node ports it needs are not wired.";

    private const string NodeGatewayUnwiredError =
        "Project funding setup is unavailable: this deployment has no shared-node gateway for project treasuries "
        + "and reserve limits, so a project cannot be given somewhere for funds to land.";

    private readonly IRepository<ProjectTokenConfig> _configRepo;
    private readonly IRepository<TokenAllocation> _allocationRepo;
    private readonly IRepository<TokenBalance> _balanceRepo;
    private readonly IRepository<PayoutRequest> _payoutRepo;
    private readonly IRepository<ProjectInvestment> _investmentRepo;
    private readonly IRepository<PlatformTreasury> _treasuryRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ITokenBalanceService _tokenBalanceService;
    private readonly ITreasuryService _treasuryService;
    private readonly ILogger<ProjectTokenService> _logger;
    private readonly IRepository<Project>? _projectRepo;
    private readonly IAlgorandService? _assets;
    private readonly IAzoaCustodialAccountService? _ownerAccounts;
    private readonly IAzoaProjectTokenNodeGateway? _nodeGateway;
    private readonly string _chainType;

    /// <summary>
    /// The provisioning dependencies carry defaults so this constructor stays callable from
    /// call sites written before self-serve funding existed. The container supplies every one
    /// of them; a null is not a permission to skip a step — <see cref="ProvisionAsync"/>
    /// refuses outright when any is missing, and no half-provisioned project is ever produced.
    /// The node gateway arrives as a collection because its adapter is registered in
    /// ArdaNova.Infrastructure (contract §4 seam) and a deployment without one must still boot.
    /// </summary>
    public ProjectTokenService(
        IRepository<ProjectTokenConfig> configRepo,
        IRepository<TokenAllocation> allocationRepo,
        IRepository<TokenBalance> balanceRepo,
        IRepository<PayoutRequest> payoutRepo,
        IRepository<ProjectInvestment> investmentRepo,
        IRepository<PlatformTreasury> treasuryRepo,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ITokenBalanceService tokenBalanceService,
        ITreasuryService treasuryService,
        ILogger<ProjectTokenService> logger,
        IRepository<Project>? projectRepo = null,
        IAlgorandService? assets = null,
        IAzoaCustodialAccountService? ownerAccounts = null,
        IEnumerable<IAzoaProjectTokenNodeGateway>? nodeGateways = null,
        IConfiguration? configuration = null)
    {
        _configRepo = configRepo;
        _allocationRepo = allocationRepo;
        _balanceRepo = balanceRepo;
        _payoutRepo = payoutRepo;
        _investmentRepo = investmentRepo;
        _treasuryRepo = treasuryRepo;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _tokenBalanceService = tokenBalanceService;
        _treasuryService = treasuryService;
        _logger = logger;
        _projectRepo = projectRepo;
        _assets = assets;
        _ownerAccounts = ownerAccounts;
        _nodeGateway = nodeGateways?.FirstOrDefault();
        _chainType = configuration?["Azoa:ChainType"] is { Length: > 0 } chainType
            ? chainType
            : DefaultChainType;
    }

    public async Task<Result<ProjectTokenConfigDto>> CreateConfigAsync(
        CreateProjectTokenConfigDto dto,
        CancellationToken ct = default)
    {
        // Check if config already exists for this project
        var existing = await _configRepo.FindOneAsync(
            c => c.projectId == dto.ProjectId,
            ct);

        if (existing != null)
            return Result<ProjectTokenConfigDto>.ValidationError(
                $"Project token config already exists for project {dto.ProjectId}");

        if (dto.AssetScale is not int assetScale || !FixedScaleAmount.IsSupportedScale(assetScale))
            return Result<ProjectTokenConfigDto>.ValidationError("Asset scale is required and must be between 0 and 18");

        var reservedSupply = (int)(dto.ReservedPercentage * dto.TotalSupply / 100);

        var entity = new ProjectTokenConfig
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            assetName = dto.AssetName,
            unitName = dto.UnitName,
            assetScale = assetScale,
            totalSupply = dto.TotalSupply,
            allocatedSupply = 0,
            distributedSupply = 0,
            reservedSupply = reservedSupply,
            fundingGoal = dto.FundingGoal,
            fundingRaised = 0,
            status = ProjectTokenStatus.PENDING,
            gateStatus = ProjectGateStatus.FUNDING,
            contributorSupply = 0,
            investorSupply = 0,
            founderSupply = reservedSupply,
            burnedSupply = 0,
            successCriteria = dto.SuccessCriteria,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _configRepo.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<ProjectTokenConfigDto>.Success(_mapper.Map<ProjectTokenConfigDto>(entity));
    }

    public async Task<Result<ProjectTokenConfigDto>> GetConfigByIdAsync(
        string id,
        CancellationToken ct = default)
    {
        var entity = await _configRepo.GetByIdAsync(id, ct);
        if (entity == null)
            return Result<ProjectTokenConfigDto>.NotFound($"Project token config {id} not found");

        return Result<ProjectTokenConfigDto>.Success(_mapper.Map<ProjectTokenConfigDto>(entity));
    }

    public async Task<Result<ProjectTokenMetadataBatchDto>> GetMetadataByIdsAsync(
        IReadOnlyCollection<string> ids,
        CancellationToken ct = default)
    {
        if (ids.Count is < 1 or > 500 ||
            ids.Any(id => string.IsNullOrWhiteSpace(id) || id.Length > 200))
        {
            return Result<ProjectTokenMetadataBatchDto>.ValidationError(
                "Request 1 to 500 project token config IDs of at most 200 characters each");
        }

        var requestedIds = ids
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var items = await _configRepo.FindProjectedAsync(
            config => requestedIds.Contains(config.id),
            config => new ProjectTokenMetadataDto
            {
                Id = config.id,
                AssetName = config.assetName,
                UnitName = config.unitName
            },
            ct);
        var byId = items.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var ordered = requestedIds
            .Where(byId.ContainsKey)
            .Select(id => byId[id])
            .ToArray();
        var missingIds = requestedIds.Where(id => !byId.ContainsKey(id)).ToArray();

        return Result<ProjectTokenMetadataBatchDto>.Success(new ProjectTokenMetadataBatchDto
        {
            Items = ordered,
            MissingIds = missingIds
        });
    }

    public async Task<Result<ProjectTokenConfigDto>> GetConfigByProjectIdAsync(
        string projectId,
        CancellationToken ct = default)
    {
        var entity = await _configRepo.FindOneAsync(
            c => c.projectId == projectId,
            ct);

        if (entity == null)
            return Result<ProjectTokenConfigDto>.NotFound(
                $"Project token config for project {projectId} not found");

        return Result<ProjectTokenConfigDto>.Success(_mapper.Map<ProjectTokenConfigDto>(entity));
    }

    // =====================================================================
    // Self-serve funding setup (W5.2)
    //
    // One server-side job, run by the project owner at publish. It creates the
    // project's stake on the shared node, resolves the treasury account the
    // project's funds land in, and registers the worker/sponsor allowances the
    // node draws against. Each leg is persisted the moment it succeeds, so a
    // retry resumes instead of re-creating; a project is only ever called
    // fundable once all three are on record.
    // =====================================================================

    /// <inheritdoc/>
    public async Task<Result<ProjectTokenFundingDto>> ProvisionAsync(
        string projectId,
        ProjectTokenDeclaration declaration,
        string requestedByUserId,
        CancellationToken ct = default)
    {
        if (_projectRepo is null || _assets is null || _ownerAccounts is null)
            return Result<ProjectTokenFundingDto>.Failure(ProvisioningUnwiredError);

        if (_nodeGateway is null)
            return Result<ProjectTokenFundingDto>.Failure(NodeGatewayUnwiredError);

        var project = await _projectRepo.GetByIdAsync(projectId, ct);
        if (project == null)
            return Result<ProjectTokenFundingDto>.NotFound($"Project {projectId} not found");

        // The owner comes from the caller's verified identity and is compared with the
        // project's recorded creator. No caller-supplied owner id is ever trusted.
        if (!string.Equals(project.createdById, requestedByUserId, StringComparison.Ordinal))
        {
            return Result<ProjectTokenFundingDto>.Forbidden(
                "Only the person who created this project can set up its funding.");
        }

        if (await RequireOwnerReadyForValueAsync(requestedByUserId, ct) is { } notReady)
            return Result<ProjectTokenFundingDto>.Forbidden(notReady);

        if (ValidateDeclaration(declaration) is { } invalid)
            return Result<ProjectTokenFundingDto>.ValidationError(invalid);

        var existing = await _configRepo.FindOneAsync(c => c.projectId == projectId, ct);
        var config = existing
            ?? await CreateDeclaredConfigAsync(projectId, declaration, requestedByUserId, ct);

        if (config.IsNodeProvisioned)
        {
            // Idempotent: a re-run of the publish job never creates a second stake.
            return Result<ProjectTokenFundingDto>.Success(DescribeFunding(project.id, config, isOwner: true));
        }

        if (config.status is ProjectTokenStatus.FROZEN or ProjectTokenStatus.DISSOLVED)
        {
            return Result<ProjectTokenFundingDto>.Conflict(
                $"This project's stake is {config.status} and funding setup cannot continue.");
        }

        if (existing is not null && string.IsNullOrWhiteSpace(config.assetId))
        {
            // Nothing has been created yet, so a re-declaration is still free: adopt it.
            // Once the stake exists the declaration is fixed and later calls only resume.
            ApplyDeclaration(config, declaration);
            await _configRepo.UpdateAsync(config, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        var totalBaseUnits = ToBaseUnits(config.totalSupply);
        if (totalBaseUnits is null)
        {
            return Result<ProjectTokenFundingDto>.ValidationError(
                "The declared number of units is too large for this platform to issue.");
        }

        if (string.IsNullOrWhiteSpace(config.assetId))
        {
            var created = await _assets.CreateFungibleASAAsync(
                config.assetName,
                config.unitName,
                totalBaseUnits.Value,
                ct);

            if (created.IsFailure || created.Value is null)
            {
                return await RecordSetupFailureAsync(
                    project.id, config, created.Error ?? "The project's stake could not be created.", ct);
            }

            config.assetId = created.Value.AssetId;
            config.assetChain = _chainType;
            config.mintTxHash = string.IsNullOrWhiteSpace(created.Value.TxHash) ? null : created.Value.TxHash;
            config.assetScale = PlatformAssetScale;
            config.provisioningFailureReason = null;
            config.updatedAt = DateTime.UtcNow;
            await _configRepo.UpdateAsync(config, ct);
            // Written before the next leg runs: the stake exists on the node from here on, and
            // a retry that created it a second time would strand the first.
            await _unitOfWork.SaveChangesAsync(ct);
        }

        if (string.IsNullOrWhiteSpace(config.treasuryAddress))
        {
            var treasury = await _nodeGateway.ResolveTreasuryAsync(
                new AzoaProjectTreasuryRequest(_chainType, project.id), ct);

            if (treasury.IsFailure || treasury.Value is null)
            {
                return await RecordSetupFailureAsync(
                    project.id,
                    config,
                    treasury.Error ?? "The project could not be given an account for funds to land in.",
                    ct);
            }

            config.treasuryChain = treasury.Value.ChainType;
            config.treasuryReference = treasury.Value.ProjectReference;
            config.treasuryAddress = treasury.Value.Address;
            config.treasuryResolvedAt = DateTime.UtcNow;
            config.provisioningFailureReason = null;
            config.updatedAt = DateTime.UtcNow;
            await _configRepo.UpdateAsync(config, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        if (config.partitionsDeclaredAt is null)
        {
            var declaredByPartition = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [ContributorPartitionName] = BaseUnitsOf(config.contributorPartitionSupply).ToString(),
                [InvestorPartitionName] = BaseUnitsOf(config.investorPartitionSupply).ToString(),
            };

            var partitions = await _nodeGateway.DeclareReservePartitionsAsync(
                new AzoaReservePartitionRequest(_chainType, config.assetId!, declaredByPartition), ct);

            if (partitions.IsFailure)
            {
                return await RecordSetupFailureAsync(
                    project.id,
                    config,
                    partitions.Error ?? "The worker and sponsor allowances could not be registered.",
                    ct);
            }

            config.partitionsDeclaredAt = DateTime.UtcNow;
            config.provisioningFailureReason = null;
            config.updatedAt = DateTime.UtcNow;
            await _configRepo.UpdateAsync(config, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        config.status = ProjectTokenStatus.ACTIVE;
        config.provisionedAt = DateTime.UtcNow;
        config.provisionedByUserId = requestedByUserId;
        config.provisioningFailureReason = null;
        config.updatedAt = DateTime.UtcNow;
        await _configRepo.UpdateAsync(config, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Project {ProjectId} funding setup completed by {UserId} on config {ConfigId}",
            project.id, requestedByUserId, config.id);

        return Result<ProjectTokenFundingDto>.Success(DescribeFunding(project.id, config, isOwner: true));
    }

    /// <inheritdoc/>
    public async Task<Result<ProjectTokenFundingDto>> GetFundingAsync(
        string projectId,
        string? viewerUserId = null,
        CancellationToken ct = default)
    {
        var isOwner = false;

        if (!string.IsNullOrWhiteSpace(viewerUserId) && _projectRepo is not null)
        {
            var project = await _projectRepo.GetByIdAsync(projectId, ct);
            if (project == null)
                return Result<ProjectTokenFundingDto>.NotFound($"Project {projectId} not found");

            isOwner = string.Equals(project.createdById, viewerUserId, StringComparison.Ordinal);
        }

        var config = await _configRepo.FindOneAsync(c => c.projectId == projectId, ct);

        return Result<ProjectTokenFundingDto>.Success(
            config is null
                ? new ProjectTokenFundingDto
                {
                    ProjectId = projectId,
                    State = ProjectTokenFundingState.NotDeclared,
                    IsFundable = false,
                    OwnerCanCompleteSetup = isOwner,
                    SetupMessage = isOwner
                        ? "You have not set up funding for this project yet."
                        : "This project is not accepting funding yet.",
                }
                : DescribeFunding(projectId, config, isOwner));
    }

    /// <summary>Uniform value-action readiness (D46): a completed identity check and a ready account, or nothing.</summary>
    private async Task<string?> RequireOwnerReadyForValueAsync(string userId, CancellationToken ct)
    {
        var status = await _ownerAccounts!.GetStatusAsync(userId, ct);
        if (status.IsFailure || status.Value is null)
        {
            return status.Error
                ?? "Your account readiness could not be confirmed, so funding setup cannot start.";
        }

        var account = status.Value;
        if (account.Ready && account.KycReady && account.WalletReady && account.IdentityReady)
            return null;

        return account.UnavailableReason
            ?? "Finish verifying your identity before setting up funding for a project.";
    }

    /// <summary>Writes a declaration onto a config whose stake does not exist yet.</summary>
    private static void ApplyDeclaration(ProjectTokenConfig config, ProjectTokenDeclaration declaration)
    {
        var founderSupply = (int)(declaration.ReservedPercentage * declaration.TotalSupply / 100);

        config.assetName = declaration.AssetName.Trim();
        config.unitName = declaration.UnitName.Trim().ToUpperInvariant();
        config.assetScale = PlatformAssetScale;
        config.totalSupply = declaration.TotalSupply;
        config.reservedSupply = founderSupply;
        config.founderSupply = founderSupply;
        config.fundingGoal = declaration.FundingGoal;
        config.contributorPartitionSupply = declaration.ContributorSupply;
        config.investorPartitionSupply = declaration.InvestorSupply;
        config.unitsDisclosure = declaration.Disclosure.Trim();
        config.successCriteria = declaration.SuccessCriteria ?? config.successCriteria;
        config.updatedAt = DateTime.UtcNow;
    }

    /// <summary>Creates the declared, still-unprovisioned config row. Never marks anything fundable.</summary>
    private async Task<ProjectTokenConfig> CreateDeclaredConfigAsync(
        string projectId,
        ProjectTokenDeclaration declaration,
        string requestedByUserId,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var config = new ProjectTokenConfig
        {
            id = Guid.NewGuid().ToString(),
            projectId = projectId,
            allocatedSupply = 0,
            distributedSupply = 0,
            fundingRaised = 0,
            status = ProjectTokenStatus.PENDING,
            gateStatus = ProjectGateStatus.FUNDING,
            contributorSupply = 0,
            investorSupply = 0,
            burnedSupply = 0,
            provisionedByUserId = requestedByUserId,
            createdAt = now,
            updatedAt = now,
        };

        ApplyDeclaration(config, declaration);

        await _configRepo.AddAsync(config, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return config;
    }

    /// <summary>Persists why setup stopped, so the owner is told what to retry instead of "not configured".</summary>
    private async Task<Result<ProjectTokenFundingDto>> RecordSetupFailureAsync(
        string projectId,
        ProjectTokenConfig config,
        string reason,
        CancellationToken ct)
    {
        config.provisioningFailureReason = reason;
        config.updatedAt = DateTime.UtcNow;
        await _configRepo.UpdateAsync(config, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogWarning(
            "Project {ProjectId} funding setup stopped on config {ConfigId}: {Reason}",
            projectId, config.id, reason);

        return Result<ProjectTokenFundingDto>.Failure(reason);
    }

    /// <summary>Projects the persisted facts onto the person-facing shape. No address, asset id or chain name.</summary>
    private static ProjectTokenFundingDto DescribeFunding(
        string projectId,
        ProjectTokenConfig config,
        bool isOwner = false)
    {
        var state = ResolveFundingState(config);
        var setupPending = state != ProjectTokenFundingState.Ready
            && state != ProjectTokenFundingState.Suspended;

        return new ProjectTokenFundingDto
        {
            ProjectId = projectId,
            ConfigId = config.id,
            State = state,
            IsFundable = config.IsFundable,
            OwnerCanCompleteSetup = isOwner && setupPending,
            SetupMessage = config.provisioningFailureReason ?? DescribeState(state),
            AssetName = config.assetName,
            UnitName = config.unitName,
            TotalSupply = config.totalSupply,
            ContributorPartitionSupply = config.contributorPartitionSupply,
            InvestorPartitionSupply = config.investorPartitionSupply,
            ContributorUnitsRemaining = Math.Max(config.contributorPartitionSupply - config.contributorSupply, 0),
            InvestorUnitsRemaining = Math.Max(config.investorPartitionSupply - config.investorSupply, 0),
            FundingGoal = config.fundingGoal,
            FundingRaised = config.fundingRaised,
            Disclosure = config.unitsDisclosure,
        };
    }

    private static string ResolveFundingState(ProjectTokenConfig config)
    {
        if (config.status is ProjectTokenStatus.FROZEN or ProjectTokenStatus.DISSOLVED)
            return ProjectTokenFundingState.Suspended;

        if (string.IsNullOrWhiteSpace(config.assetId))
            return ProjectTokenFundingState.AwaitingUnits;

        if (string.IsNullOrWhiteSpace(config.treasuryAddress))
            return ProjectTokenFundingState.AwaitingTreasury;

        if (config.partitionsDeclaredAt is null || config.status != ProjectTokenStatus.ACTIVE)
            return ProjectTokenFundingState.AwaitingAllowances;

        return ProjectTokenFundingState.Ready;
    }

    private static string DescribeState(string state) => state switch
    {
        ProjectTokenFundingState.Ready => "This project can accept funding.",
        ProjectTokenFundingState.Suspended => "Funding for this project is suspended.",
        ProjectTokenFundingState.AwaitingUnits => "The project's stake is still being created.",
        ProjectTokenFundingState.AwaitingTreasury =>
            "The project does not yet have an account for funds to land in.",
        _ => "Funding setup for this project is not finished yet.",
    };

    /// <summary>The one refusal every allocation path runs: a project with no stake, no treasury
    /// or no registered allowances has nothing to issue and nowhere for funds to land.</summary>
    private static string? DescribeUnfundable(ProjectTokenConfig config)
    {
        if (config.IsFundable)
            return null;

        return config.IsNodeProvisioned
            ? $"This project's stake is {config.status} and cannot be allocated."
            : "Funding setup for this project is not finished, so no stake can be allocated yet.";
    }

    /// <summary>Mirrors the node's partition check so ArdaNova never promises units the node would refuse.</summary>
    private static string? DescribePartitionOversubscription(
        string allowanceName,
        int declared,
        int committed,
        int requested)
    {
        if (requested <= 0)
            return $"A {allowanceName} allocation must be at least one unit.";

        if (committed + (long)requested > declared)
        {
            return $"The {allowanceName} allowance is oversubscribed: {requested} units requested, "
                + $"{Math.Max(declared - committed, 0)} of {declared} still available.";
        }

        return null;
    }

    /// <summary>Declaration rules. Everything a person types is checked here, once.</summary>
    private static string? ValidateDeclaration(ProjectTokenDeclaration declaration)
    {
        var assetName = declaration.AssetName?.Trim() ?? string.Empty;
        if (assetName.Length is < 3 or > MaximumAssetNameLength)
            return $"Give the stake a name between 3 and {MaximumAssetNameLength} characters.";

        var unitName = declaration.UnitName?.Trim() ?? string.Empty;
        if (unitName.Length is < 2 or > MaximumUnitNameLength
            || !unitName.All(character => character < 128 && char.IsLetterOrDigit(character)))
        {
            return $"Give the stake a short label of 2 to {MaximumUnitNameLength} letters or digits.";
        }

        if (declaration.TotalSupply < 1)
            return "A project must declare at least one unit of stake.";

        if (declaration.ContributorSupply < 0 || declaration.InvestorSupply < 0)
            return "Worker and sponsor allowances cannot be negative.";

        if (declaration.ContributorSupply + (long)declaration.InvestorSupply < 1)
            return "Set aside at least one unit for workers or sponsors, or nobody can take part.";

        if (declaration.ReservedPercentage is < 0 or > 100)
            return "The founder reserve must be between 0 and 100 percent.";

        var founderSupply = (long)(declaration.ReservedPercentage * declaration.TotalSupply / 100);
        var committed = declaration.ContributorSupply + (long)declaration.InvestorSupply + founderSupply;
        if (committed > declaration.TotalSupply)
        {
            return $"The worker, sponsor and founder shares add up to {committed} units, "
                + $"more than the {declaration.TotalSupply} declared.";
        }

        if (declaration.FundingGoal <= 0 || double.IsNaN(declaration.FundingGoal)
            || double.IsInfinity(declaration.FundingGoal))
        {
            return "Set a funding goal above zero.";
        }

        var disclosure = declaration.Disclosure?.Trim() ?? string.Empty;
        if (disclosure.Length is < MinimumDisclosureLength or > MaximumDisclosureLength)
        {
            return $"Write {MinimumDisclosureLength} to {MaximumDisclosureLength} characters telling "
                + "sponsors and workers what their stake means and when it becomes theirs.";
        }

        return null;
    }

    /// <summary>Whole declared units expressed in the platform's issuing scale; null when out of range.</summary>
    private static ulong? ToBaseUnits(int wholeUnits)
    {
        var baseUnits = BaseUnitsOf(wholeUnits);
        return baseUnits > ulong.MaxValue ? null : (ulong)baseUnits;
    }

    private static BigInteger BaseUnitsOf(int wholeUnits) =>
        new BigInteger(wholeUnits) * BigInteger.Pow(10, PlatformAssetScale);

    public async Task<Result<TokenAllocationDto>> AllocateToPbiAsync(
        string projectTokenConfigId,
        CreateTokenAllocationDto dto,
        CancellationToken ct = default)
    {
        var config = await _configRepo.GetByIdAsync(projectTokenConfigId, ct);
        if (config == null)
            return Result<TokenAllocationDto>.NotFound(
                $"Project token config {projectTokenConfigId} not found");

        if (DescribeUnfundable(config) is { } unfundable)
            return Result<TokenAllocationDto>.ValidationError(unfundable);

        // Validate gateStatus >= ACTIVE
        if (config.gateStatus < ProjectGateStatus.ACTIVE)
            return Result<TokenAllocationDto>.ValidationError(
                $"Cannot allocate to task until project gate status is ACTIVE. Current: {config.gateStatus}");

        // Calculate token amount
        var tokenAmount = (int)(config.totalSupply * dto.EquityPercentage / 100);

        // The declared contributor partition is the same allowance the node polices on its
        // reserve ledger. Checking it here keeps ArdaNova's own books from promising units
        // the node would refuse to draw.
        if (DescribePartitionOversubscription(
                "worker stake",
                config.contributorPartitionSupply,
                config.contributorSupply,
                tokenAmount) is { } contributorRefusal)
        {
            return Result<TokenAllocationDto>.ValidationError(contributorRefusal);
        }

        // Validate supply available
        var availableSupply = config.totalSupply - config.contributorSupply - config.investorSupply
            - config.founderSupply - config.burnedSupply;
        if (tokenAmount > availableSupply)
            return Result<TokenAllocationDto>.ValidationError(
                $"Insufficient supply. Requested: {tokenAmount}, Available: {availableSupply}");

        var allocation = new TokenAllocation
        {
            id = Guid.NewGuid().ToString(),
            projectTokenConfigId = projectTokenConfigId,
            pbiId = dto.PbiId,
            recipientUserId = null,
            equityPercentage = dto.EquityPercentage,
            tokenAmount = tokenAmount,
            status = AllocationStatus.RESERVED,
            holderClass = TokenHolderClass.CONTRIBUTOR,
            isLiquid = false,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _allocationRepo.AddAsync(allocation, ct);

        // Increment contributorSupply
        config.contributorSupply += tokenAmount;
        config.allocatedSupply += tokenAmount;
        config.updatedAt = DateTime.UtcNow;
        await _configRepo.UpdateAsync(config, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return Result<TokenAllocationDto>.Success(_mapper.Map<TokenAllocationDto>(allocation));
    }

    public async Task<Result<TokenAllocationDto>> AllocateToInvestorAsync(
        string projectTokenConfigId,
        CreateInvestorAllocationDto dto,
        CancellationToken ct = default,
        string? stripePaymentIntentId = null)
    {
        var config = await _configRepo.GetByIdAsync(projectTokenConfigId, ct);
        if (config == null)
            return Result<TokenAllocationDto>.NotFound(
                $"Project token config {projectTokenConfigId} not found");

        if (DescribeUnfundable(config) is { } unfundable)
            return Result<TokenAllocationDto>.ValidationError(unfundable);

        // Same node-side allowance, sponsor side. A partition policed only in this database
        // can be overdrawn by the other caller, so both halves check the declared cap.
        if (DescribePartitionOversubscription(
                "sponsor stake",
                config.investorPartitionSupply,
                config.investorSupply,
                dto.TokenAmount) is { } investorRefusal)
        {
            return Result<TokenAllocationDto>.ValidationError(investorRefusal);
        }

        // Validate supply available
        var availableSupply = config.totalSupply - config.contributorSupply - config.investorSupply
            - config.founderSupply - config.burnedSupply;
        if (dto.TokenAmount > availableSupply)
            return Result<TokenAllocationDto>.ValidationError(
                $"Insufficient supply. Requested: {dto.TokenAmount}, Available: {availableSupply}");

        // Calculate equity percentage
        var equityPercentage = (double)dto.TokenAmount / config.totalSupply * 100;

        var allocation = new TokenAllocation
        {
            id = Guid.NewGuid().ToString(),
            projectTokenConfigId = projectTokenConfigId,
            pbiId = null,
            recipientUserId = dto.UserId,
            equityPercentage = equityPercentage,
            tokenAmount = dto.TokenAmount,
            status = AllocationStatus.RESERVED,
            holderClass = TokenHolderClass.INVESTOR,
            isLiquid = false,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _allocationRepo.AddAsync(allocation, ct);

        // Update fundingRaised and investorSupply
        config.fundingRaised += dto.UsdAmount;
        config.investorSupply += dto.TokenAmount;
        config.allocatedSupply += dto.TokenAmount;
        config.updatedAt = DateTime.UtcNow;
        await _configRepo.UpdateAsync(config, ct);

        // Create ProjectInvestment record
        var investment = new ProjectInvestment
        {
            id = Guid.NewGuid().ToString(),
            projectTokenConfigId = projectTokenConfigId,
            userId = dto.UserId,
            usdAmount = dto.UsdAmount,
            tokenAmount = dto.TokenAmount,
            stripePaymentIntentId = stripePaymentIntentId,
            investedAt = DateTime.UtcNow,
            protectionEligible = true,
            protectionPaidOut = false
        };

        await _investmentRepo.AddAsync(investment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<TokenAllocationDto>.Success(_mapper.Map<TokenAllocationDto>(allocation));
    }

    public async Task<Result<TokenAllocationDto>> AllocateToFounderAsync(
        string projectTokenConfigId,
        CreateFounderAllocationDto dto,
        CancellationToken ct = default)
    {
        var config = await _configRepo.GetByIdAsync(projectTokenConfigId, ct);
        if (config == null)
            return Result<TokenAllocationDto>.NotFound(
                $"Project token config {projectTokenConfigId} not found");

        if (DescribeUnfundable(config) is { } unfundable)
            return Result<TokenAllocationDto>.ValidationError(unfundable);

        // Calculate token amount
        var tokenAmount = (int)(config.totalSupply * dto.EquityPercentage / 100);

        // Validate within reservedSupply
        if (tokenAmount > config.founderSupply)
            return Result<TokenAllocationDto>.ValidationError(
                $"Insufficient founder/reserved supply. Requested: {tokenAmount}, Available: {config.founderSupply}");

        var allocation = new TokenAllocation
        {
            id = Guid.NewGuid().ToString(),
            projectTokenConfigId = projectTokenConfigId,
            pbiId = null,
            recipientUserId = dto.UserId,
            equityPercentage = dto.EquityPercentage,
            tokenAmount = tokenAmount,
            status = AllocationStatus.RESERVED,
            holderClass = TokenHolderClass.FOUNDER,
            isLiquid = false,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _allocationRepo.AddAsync(allocation, ct);

        // Increment allocatedSupply (founderSupply already includes the reserved amount)
        config.allocatedSupply += tokenAmount;
        config.updatedAt = DateTime.UtcNow;
        await _configRepo.UpdateAsync(config, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return Result<TokenAllocationDto>.Success(_mapper.Map<TokenAllocationDto>(allocation));
    }

    public async Task<Result<TokenAllocationDto>> DistributeAsync(
        string allocationId,
        string recipientUserId,
        CancellationToken ct = default)
    {
        var allocation = await _allocationRepo.GetByIdAsync(allocationId, ct);
        if (allocation == null)
            return Result<TokenAllocationDto>.NotFound($"Allocation {allocationId} not found");

        if (allocation.status != AllocationStatus.RESERVED)
            return Result<TokenAllocationDto>.ValidationError(
                $"Only RESERVED allocations can be distributed. Current status: {allocation.status}");

        var config = await _configRepo.GetByIdAsync(allocation.projectTokenConfigId, ct);
        if (config == null)
            return Result<TokenAllocationDto>.NotFound(
                $"Project token config {allocation.projectTokenConfigId} not found");

        // Determine isLiquid based on holderClass + gateStatus
        var isLiquid = TokenLiquidityPolicy.IsLiquid(allocation.holderClass, config.gateStatus);

        // Update allocation
        allocation.recipientUserId = recipientUserId;
        allocation.status = AllocationStatus.DISTRIBUTED;
        allocation.isLiquid = isLiquid;
        allocation.distributedAt = DateTime.UtcNow;
        allocation.updatedAt = DateTime.UtcNow;
        await _allocationRepo.UpdateAsync(allocation, ct);

        // Update distributedSupply
        config.distributedSupply += allocation.tokenAmount;
        config.updatedAt = DateTime.UtcNow;
        await _configRepo.UpdateAsync(config, ct);

        // Credit token balance
        var creditResult = await _tokenBalanceService.CreditAsync(
            recipientUserId,
            allocation.projectTokenConfigId,
            allocation.tokenAmount,
            allocation.holderClass,
            ct);

        if (creditResult.IsFailure)
        {
            _logger.LogError("Failed to credit token balance for user {UserId}: {Error}",
                recipientUserId, creditResult.Error);
            return Result<TokenAllocationDto>.Failure(
                $"Distribution succeeded but failed to credit balance: {creditResult.Error}");
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return Result<TokenAllocationDto>.Success(_mapper.Map<TokenAllocationDto>(allocation));
    }

    public async Task<Result<TokenAllocationDto>> RevokeAllocationAsync(
        string allocationId,
        CancellationToken ct = default)
    {
        var allocation = await _allocationRepo.GetByIdAsync(allocationId, ct);
        if (allocation == null)
            return Result<TokenAllocationDto>.NotFound($"Allocation {allocationId} not found");

        if (allocation.status == AllocationStatus.REVOKED)
            return Result<TokenAllocationDto>.ValidationError("Allocation is already revoked");

        if (allocation.status == AllocationStatus.BURNED)
            return Result<TokenAllocationDto>.ValidationError("Cannot revoke burned allocation");

        var config = await _configRepo.GetByIdAsync(allocation.projectTokenConfigId, ct);
        if (config == null)
            return Result<TokenAllocationDto>.NotFound(
                $"Project token config {allocation.projectTokenConfigId} not found");

        // Set status to REVOKED
        allocation.status = AllocationStatus.REVOKED;
        allocation.updatedAt = DateTime.UtcNow;
        await _allocationRepo.UpdateAsync(allocation, ct);

        // Decrement appropriate supply counter
        switch (allocation.holderClass)
        {
            case TokenHolderClass.CONTRIBUTOR:
                config.contributorSupply -= allocation.tokenAmount;
                break;
            case TokenHolderClass.INVESTOR:
                config.investorSupply -= allocation.tokenAmount;
                break;
            case TokenHolderClass.FOUNDER:
                // For founder, we don't decrement founderSupply as it's reserved
                break;
        }

        config.allocatedSupply -= allocation.tokenAmount;
        config.updatedAt = DateTime.UtcNow;
        await _configRepo.UpdateAsync(config, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return Result<TokenAllocationDto>.Success(_mapper.Map<TokenAllocationDto>(allocation));
    }

    public async Task<Result<IReadOnlyList<TokenAllocationDto>>> GetAllocationsByProjectAsync(
        string projectTokenConfigId,
        CancellationToken ct = default)
    {
        var allocations = await _allocationRepo.FindAsync(
            a => a.projectTokenConfigId == projectTokenConfigId,
            ct);

        var dtos = _mapper.Map<IReadOnlyList<TokenAllocationDto>>(allocations);
        return Result<IReadOnlyList<TokenAllocationDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<TokenAllocationDto>>> GetAllocationsByPbiAsync(
        string pbiId,
        CancellationToken ct = default)
    {
        var allocations = await _allocationRepo.FindAsync(
            a => a.pbiId == pbiId,
            ct);

        var dtos = _mapper.Map<IReadOnlyList<TokenAllocationDto>>(allocations);
        return Result<IReadOnlyList<TokenAllocationDto>>.Success(dtos);
    }

    public async Task<Result<ProjectTokenConfigDto>> GetSupplyBreakdownAsync(
        string projectTokenConfigId,
        CancellationToken ct = default)
    {
        var config = await _configRepo.GetByIdAsync(projectTokenConfigId, ct);
        if (config == null)
            return Result<ProjectTokenConfigDto>.NotFound(
                $"Project token config {projectTokenConfigId} not found");

        return Result<ProjectTokenConfigDto>.Success(_mapper.Map<ProjectTokenConfigDto>(config));
    }

    public async Task<Result<bool>> BurnFounderTokensAsync(
        string projectTokenConfigId,
        CancellationToken ct = default)
    {
        var config = await _configRepo.GetByIdAsync(projectTokenConfigId, ct);
        if (config == null)
            return Result<bool>.NotFound($"Project token config {projectTokenConfigId} not found");

        // Find all FOUNDER allocations
        var founderAllocations = await _allocationRepo.FindAsync(
            a => a.projectTokenConfigId == projectTokenConfigId
                && a.holderClass == TokenHolderClass.FOUNDER
                && a.status != AllocationStatus.BURNED,
            ct);

        if (!founderAllocations.Any())
        {
            _logger.LogInformation("No founder allocations to burn for project token config {ConfigId}",
                projectTokenConfigId);
            return Result<bool>.Success(true);
        }

        var totalBurned = 0;

        foreach (var allocation in founderAllocations)
        {
            // Set status = BURNED, burnedAt
            allocation.status = AllocationStatus.BURNED;
            allocation.burnedAt = DateTime.UtcNow;
            allocation.updatedAt = DateTime.UtcNow;
            await _allocationRepo.UpdateAsync(allocation, ct);

            totalBurned += allocation.tokenAmount;

            // Zero token balances if distributed
            if (allocation.recipientUserId != null)
            {
                var balances = await _balanceRepo.FindAsync(
                    b => b.userId == allocation.recipientUserId
                        && b.projectTokenConfigId == projectTokenConfigId
                        && b.holderClass == TokenHolderClass.FOUNDER,
                    ct);

                foreach (var balance in balances)
                {
                    balance.balance = 0;
                    balance.lockedBalance = 0;
                    balance.updatedAt = DateTime.UtcNow;
                    await _balanceRepo.UpdateAsync(balance, ct);
                }
            }
        }

        // Increment burnedSupply, decrement founderSupply
        config.burnedSupply += totalBurned;
        config.founderSupply -= totalBurned;
        config.updatedAt = DateTime.UtcNow;
        await _configRepo.UpdateAsync(config, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Burned {Count} founder allocations ({Total} tokens) for project token config {ConfigId}",
            founderAllocations.Count, totalBurned, projectTokenConfigId);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ProcessInvestorTrustProtectionAsync(
        string projectTokenConfigId,
        CancellationToken ct = default)
    {
        var config = await _configRepo.GetByIdAsync(projectTokenConfigId, ct);
        if (config == null)
            return Result<bool>.NotFound($"Project token config {projectTokenConfigId} not found");

        // Get platform treasury to get trustProtectionRate
        var treasuries = await _treasuryRepo.GetAllAsync(ct);
        var treasury = treasuries.FirstOrDefault();
        if (treasury == null)
            return Result<bool>.Failure("Platform treasury not found");

        // Find all ProjectInvestments that are eligible and not yet paid out
        var investments = await _investmentRepo.FindAsync(
            i => i.projectTokenConfigId == projectTokenConfigId
                && i.protectionEligible
                && !i.protectionPaidOut,
            ct);

        if (!investments.Any())
        {
            _logger.LogInformation("No eligible investments for trust protection in project token config {ConfigId}",
                projectTokenConfigId);
            return Result<bool>.Success(true);
        }

        var totalProtection = 0.0;

        foreach (var investment in investments)
        {
            // Calculate protectionAmount = usdAmount * trustProtectionRate
            var protectionAmount = investment.usdAmount * treasury.trustProtectionRate;

            // Create PayoutRequest
            var payoutRequest = new PayoutRequest
            {
                id = Guid.NewGuid().ToString(),
                userId = investment.userId,
                sourceProjectTokenConfigId = projectTokenConfigId,
                sourceTokenAmount = 0, // Trust protection is not from token conversion
                usdAmount = protectionAmount,
                status = PayoutStatus.PENDING,
                holderClass = TokenHolderClass.INVESTOR,
                gateStatusAtRequest = config.gateStatus,
                requestedAt = DateTime.UtcNow
            };

            await _payoutRepo.AddAsync(payoutRequest, ct);

            // Update investment
            investment.protectionAmount = protectionAmount;
            investment.protectionPaidOut = true;
            investment.protectionPaidAt = DateTime.UtcNow;
            await _investmentRepo.UpdateAsync(investment, ct);

            totalProtection += protectionAmount;
        }

        await _unitOfWork.SaveChangesAsync(ct);

        // Call ITreasuryService for index fund debit (via ProcessFundingInflowAsync with negative amount or custom method)
        // Note: ITreasuryService doesn't have a direct debit method in the interface provided,
        // so we'll log this for now. In production, you might need to add a DebitIndexFundAsync method.
        _logger.LogInformation(
            "Processed trust protection for {Count} investments ({Total} USD) for project token config {ConfigId}. " +
            "Treasury debit required: {Amount} USD",
            investments.Count, totalProtection, projectTokenConfigId, totalProtection);

        return Result<bool>.Success(true);
    }

    public async Task<Result<IReadOnlyList<ProjectInvestmentDto>>> GetInvestorsByProjectAsync(
        string projectTokenConfigId,
        CancellationToken ct = default)
    {
        var investments = await _investmentRepo.FindAsync(
            i => i.projectTokenConfigId == projectTokenConfigId,
            ct);

        var dtos = _mapper.Map<IReadOnlyList<ProjectInvestmentDto>>(investments);
        return Result<IReadOnlyList<ProjectInvestmentDto>>.Success(dtos);
    }

}

/// <summary>
/// Self-serve funding setup, exposed under its own interface so callers depend on it
/// explicitly (the <c>IProjectVisibility</c> precedent in <c>Application/DependencyInjection.cs</c>).
/// <see cref="ProjectTokenService"/> implements both this and <c>IProjectTokenService</c> on
/// one scoped instance.
/// </summary>
public interface IProjectTokenProvisioning
{
    /// <summary>
    /// Runs the publish-time funding job for one project: create the stake, resolve the
    /// project's treasury account, register the worker and sponsor allowances. Idempotent —
    /// a completed project is returned unchanged, a half-finished one resumes where it
    /// stopped, and nothing is ever reported fundable while a leg is missing.
    /// </summary>
    Task<Result<ProjectTokenFundingDto>> ProvisionAsync(
        string projectId,
        ProjectTokenDeclaration declaration,
        string requestedByUserId,
        CancellationToken ct = default);

    /// <summary>What a project's funding setup looks like right now; <paramref name="viewerUserId"/>
    /// decides only whether the owner's own setup actions are offered.</summary>
    Task<Result<ProjectTokenFundingDto>> GetFundingAsync(
        string projectId,
        string? viewerUserId = null,
        CancellationToken ct = default);
}

/// <summary>
/// The node-side half of funding setup: the treasury account a project's funds land in and
/// the named allowances the node draws stake from. The stake itself is created through
/// <see cref="IAlgorandService.CreateFungibleASAAsync"/>, which the <c>Algorand:Provider</c>
/// seam already routes to the shared node.
/// <para>
/// Zero implementations is a supported state: a deployment that has not wired the adapter
/// still boots and refuses funding setup with a named reason. It never half-provisions.
/// </para>
/// </summary>
public interface IAzoaProjectTokenNodeGateway
{
    /// <summary>Resolve-or-create this project's custodial treasury account on the node. Idempotent.</summary>
    Task<Result<AzoaProjectTreasury>> ResolveTreasuryAsync(
        AzoaProjectTreasuryRequest request,
        CancellationToken ct = default);

    /// <summary>Declare the named allowances on the node's reserve ledger for this stake.</summary>
    Task<Result<AzoaReservePartitions>> DeclareReservePartitionsAsync(
        AzoaReservePartitionRequest request,
        CancellationToken ct = default);
}

/// <summary>Address of one project's treasury: chain plus this tenant's own opaque project reference.</summary>
public sealed record AzoaProjectTreasuryRequest(string ChainType, string ProjectReference);

/// <summary>The node's answer: the account funding ARDA lands in.</summary>
public sealed record AzoaProjectTreasury(
    string ChainType,
    string ProjectReference,
    string Address,
    string Status);

/// <summary>
/// Allowance declaration for one stake. Amounts are BASE units as decimal strings — the node's
/// reserve ledger counts base units, and a string keeps a large count exact across the wire.
/// </summary>
public sealed record AzoaReservePartitionRequest(
    string ChainType,
    string AssetId,
    IReadOnlyDictionary<string, string> DeclaredBaseUnitsByPartition);

/// <summary>What the node recorded, echoed back for the caller to compare against what it sent.</summary>
public sealed record AzoaReservePartitions(
    string AssetId,
    IReadOnlyDictionary<string, string> DeclaredBaseUnitsByPartition);

/// <summary>What a project owner declares once, at publish, in their own words and units.</summary>
public sealed record ProjectTokenDeclaration
{
    /// <summary>Display name of the project's stake, e.g. "Riverside Allotment Stake".</summary>
    public required string AssetName { get; init; }

    /// <summary>Short label people see beside a quantity, e.g. "PLOT". 2-8 letters or digits.</summary>
    public required string UnitName { get; init; }

    /// <summary>How many whole units of stake exist, ever.</summary>
    public required int TotalSupply { get; init; }

    /// <summary>Units set aside for the people who do the work.</summary>
    public required int ContributorSupply { get; init; }

    /// <summary>Units set aside for the people who sponsor the work.</summary>
    public required int InvestorSupply { get; init; }

    /// <summary>What the project is asking for, in platform units.</summary>
    public required double FundingGoal { get; init; }

    /// <summary>
    /// What a stake means and when it becomes the holder's — shown before anyone commits
    /// funds. Required: a project may not raise money without saying what it is offering.
    /// </summary>
    public required string Disclosure { get; init; }

    /// <summary>Percentage of the whole held back for the founders.</summary>
    public double ReservedPercentage { get; init; }

    /// <summary>What counts as this project having succeeded.</summary>
    public string? SuccessCriteria { get; init; }
}

/// <summary>Funding setup states, in the order they are reached.</summary>
public static class ProjectTokenFundingState
{
    /// <summary>The owner has not declared a stake for this project.</summary>
    public const string NotDeclared = "NotDeclared";

    /// <summary>Declared, but the stake itself has not been created yet.</summary>
    public const string AwaitingUnits = "AwaitingUnits";

    /// <summary>Stake exists; the project still has nowhere for funds to land.</summary>
    public const string AwaitingTreasury = "AwaitingTreasury";

    /// <summary>Treasury exists; the worker and sponsor allowances are not registered yet.</summary>
    public const string AwaitingAllowances = "AwaitingAllowances";

    /// <summary>Every leg is on record; the project can accept funding.</summary>
    public const string Ready = "Ready";

    /// <summary>Frozen or dissolved; no funding, whatever else is on record.</summary>
    public const string Suspended = "Suspended";
}

/// <summary>
/// A project's funding setup as a person reads it. Deliberately carries no address, asset id
/// or chain name: a sponsor sees "this project can accept funding" and a quantity of units,
/// never a wallet.
/// </summary>
public sealed record ProjectTokenFundingDto
{
    public required string ProjectId { get; init; }

    public string? ConfigId { get; init; }

    /// <summary>One of <see cref="ProjectTokenFundingState"/>.</summary>
    public required string State { get; init; }

    /// <summary>The single answer every funding surface asks for.</summary>
    public required bool IsFundable { get; init; }

    /// <summary>True only for the project's owner while setup is unfinished.</summary>
    public required bool OwnerCanCompleteSetup { get; init; }

    /// <summary>Plain-language state, or the exact reason the last setup attempt stopped.</summary>
    public string? SetupMessage { get; init; }

    public string? AssetName { get; init; }

    public string? UnitName { get; init; }

    public int TotalSupply { get; init; }

    public int ContributorPartitionSupply { get; init; }

    public int InvestorPartitionSupply { get; init; }

    public int ContributorUnitsRemaining { get; init; }

    public int InvestorUnitsRemaining { get; init; }

    public double FundingGoal { get; init; }

    public double FundingRaised { get; init; }

    /// <summary>The owner's disclosure, shown before anyone commits funds.</summary>
    public string? Disclosure { get; init; }
}
