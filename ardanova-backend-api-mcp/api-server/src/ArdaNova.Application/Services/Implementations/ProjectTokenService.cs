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

    /// <summary>What the person was trying to do, for the shared gate's own refusal copy.</summary>
    private const string FundingSetupAction = "set up funding for a project";

    /// <summary>What the worker was trying to do, for the shared gate's own refusal copy.</summary>
    private const string AwardHandoverAction = "receive this award";

    private const int MaximumAssetNameLength = 32;
    private const int MaximumUnitNameLength = 8;
    private const int MinimumDisclosureLength = 20;
    private const int MaximumDisclosureLength = 2000;

    private const string ProvisioningUnwiredError =
        "Project funding setup is unavailable in this deployment: the shared-node ports it needs are not wired.";

    private const string NodeGatewayUnwiredError =
        "Project funding setup is unavailable: this deployment has no shared-node gateway for project treasuries "
        + "and reserve limits, so a project cannot be given somewhere for funds to land.";

    private const string HoldGatewayUnwiredError =
        "Approving completed work is unavailable in this deployment: there is no shared-node hold gateway, "
        + "so the units reserved for this worker cannot be moved into their own window.";

    private const string AwardUnwiredError =
        "Approving completed work is unavailable in this deployment: the ports that link an award to its "
        + "project, its task and its agreement are not wired.";

    /// <summary>
    /// Default length of the worker's window when the operator has declared none
    /// (<c>Azoa:AwardWindowDays</c>). The window exists so an award is restricted until it is
    /// liquid, which defers the taxable event to the moment the worker can actually realise it
    /// (gotcha 38). It is NOT a chargeback window: reward units come out of the platform
    /// reserve and were never bought with a card, so there is nothing to reverse. Counsel
    /// confirms the length before the first payout.
    /// </summary>
    private const int DefaultAwardWindowDays = 90;

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
    private readonly IAzoaValueEligibilityService _valueEligibility;
    private readonly IAzoaProjectTokenNodeGateway? _nodeGateway;
    private readonly IAzoaHoldGateway? _holds;
    private readonly IRepository<TaskCommerceAgreement>? _agreementRepo;
    private readonly IRepository<ProjectTask>? _taskRepo;
    private readonly string _chainType;
    private readonly int _awardWindowDays;

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
        IAzoaValueEligibilityService valueEligibility,
        IRepository<Project>? projectRepo = null,
        IAlgorandService? assets = null,
        IAzoaCustodialAccountService? ownerAccounts = null,
        IEnumerable<IAzoaProjectTokenNodeGateway>? nodeGateways = null,
        IConfiguration? configuration = null,
        IEnumerable<IAzoaHoldGateway>? holdGateways = null,
        IRepository<TaskCommerceAgreement>? agreementRepo = null,
        IRepository<ProjectTask>? taskRepo = null)
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
        _valueEligibility = valueEligibility
            ?? throw new ArgumentNullException(nameof(valueEligibility));
        _projectRepo = projectRepo;
        _assets = assets;
        _ownerAccounts = ownerAccounts;
        _nodeGateway = nodeGateways?.FirstOrDefault();
        _holds = holdGateways?.FirstOrDefault();
        _agreementRepo = agreementRepo;
        _taskRepo = taskRepo;
        _chainType = configuration?["Azoa:ChainType"] is { Length: > 0 } chainType
            ? chainType
            : DefaultChainType;
        _awardWindowDays =
            int.TryParse(configuration?["Azoa:AwardWindowDays"], out var declaredWindow) && declaredWindow > 0
                ? declaredWindow
                : DefaultAwardWindowDays;
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

        // The third door that writes `fundingGoal`. The owner's declaration and the project's own
        // ask both refuse a non-dollar figure; this one used to take a bare double with no unit on
        // the wire at all, so an operator could put a platform-unit ask into the column the
        // opening price divides. The refusal is stated in the same words at all three.
        if (ProjectFundingLedger.DescribeUnsupportedGoalUnit(dto.FundingGoalUnit) is { } badGoalUnit)
            return Result<ProjectTokenConfigDto>.ValidationError(badGoalUnit);

        // Same door, the other half of the rule: an operator could put $0.001 or $1e12 into the
        // column the opening price divides, in the right unit, and nothing refused it.
        if (ProjectFundingLedger.DescribeUnusableGoalAmount(dto.FundingGoal) is { } badGoalAmount)
            return Result<ProjectTokenConfigDto>.ValidationError(badGoalAmount);

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

        var ownerReadiness = await _valueEligibility.RequireReadyAsync(
            requestedByUserId, FundingSetupAction, ct);
        if (!ownerReadiness.IsSuccess)
        {
            return ValueEligibilityFailure
                .Propagate<AzoaCustodialAccountStatusDto, ProjectTokenFundingDto>(ownerReadiness);
        }

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
            // Keyed on the row, not the attempt: every retry of this declaration replays the
            // node's one creation, and a new config row is a new stake (W5.2 stable key).
            var created = await _assets.CreateFungibleASAAsync(
                config.assetName,
                config.unitName,
                totalBaseUnits.Value,
                GenesisIdempotencyKey(config.id),
                ct);

            if (created.IsFailure || created.Value is null)
            {
                return await RecordSetupFailureAsync(
                    project.id, config, created.Error ?? "The project's stake could not be created.", ct);
            }

            config.assetId = created.Value.AssetId;
            // The chain the node reports it minted on (a Simulated node redirects Algorand), else configured.
            config.assetChain = string.IsNullOrWhiteSpace(created.Value.ChainType) ? _chainType : created.Value.ChainType;
            config.mintTxHash = string.IsNullOrWhiteSpace(created.Value.TxHash) ? null : created.Value.TxHash;
            config.assetScale = PlatformAssetScale;
            config.provisioningFailureReason = null;
            config.updatedAt = DateTime.UtcNow;
            await _configRepo.UpdateAsync(config, ct);
            // Written before the next leg runs: the stake exists on the node from here on, and
            // a retry that created it a second time would strand the first.
            await _unitOfWork.SaveChangesAsync(ct);
        }

        // Treasury and partitions live on the chain the stake was minted on, never re-read from config.
        var assetChain = string.IsNullOrWhiteSpace(config.assetChain) ? _chainType : config.assetChain!;

        if (string.IsNullOrWhiteSpace(config.treasuryAddress))
        {
            var treasury = await _nodeGateway.ResolveTreasuryAsync(
                new AzoaProjectTreasuryRequest(assetChain, project.id), ct);

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
                new AzoaReservePartitionRequest(assetChain, config.assetId!, declaredByPartition), ct);

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

    /// <summary>The one request key a config row's stake is ever created under.</summary>
    private static string GenesisIdempotencyKey(string configId) => $"project-token-genesis:{configId}";

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

    /// <summary>
    /// Mirrors the node's partition check so ArdaNova never promises units the node would refuse.
    /// One implementation, in the domain, because the sponsorship path draws the same allowances.
    /// </summary>
    private static string? DescribePartitionOversubscription(
        string allowanceName,
        int declared,
        int committed,
        int requested) =>
        ProjectStakePartitions.DescribeOversubscription(allowanceName, declared, committed, requested);

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

        if (declaration.ReservedPercentage is < 0 or > 100)
            return "The founder reserve must be between 0 and 100 percent.";

        var founderSupply = (long)(declaration.ReservedPercentage * declaration.TotalSupply / 100);
        var committed = declaration.ContributorSupply + (long)declaration.InvestorSupply + founderSupply;
        if (committed > declaration.TotalSupply)
        {
            return $"The worker, sponsor and founder shares add up to {committed} units, "
                + $"more than the {declaration.TotalSupply} declared.";
        }

        // Magnitude and cent precision, not just "above zero". The wizard refuses these in the
        // browser; funding-tab.tsx is a second field that did not, and the API takes the number
        // directly, so the bound has to be stated at the door that writes the column.
        if (ProjectFundingLedger.DescribeUnusableGoalAmount(declaration.FundingGoal) is { } badGoal)
            return badGoal;

        // The ask is priced over the sponsor stake and nothing else
        // (<see cref="ProjectRaiseOffering"/>), so an ask with no sponsor stake behind it is an
        // ask nobody can answer: the offering is unpriceable, no sponsorship can settle, and the
        // gate that would free the workers' units never clears. Refused at the door rather than
        // discovered by the first sponsor.
        // This subsumes the older "workers OR sponsors" rule, which was true when the ask was
        // spread over the whole supply and is not any more: worker units are compensation, they
        // are not on sale, so they cannot answer an ask.
        if (declaration.InvestorSupply < 1)
        {
            return "Set aside at least one unit for sponsors. The amount you are asking for is "
                + "spread across the sponsor units, so a project asking for funds with none set "
                + "aside has nothing to offer and could never reach its goal.";
        }

        // The unit is a CONTROL, not a label: `fundingGoal` sits beside `fundingRaised`, which
        // accumulates dollars, and the opening price divides one into the other. This refusal is
        // the control; the shared reader downstream cannot detect a unit in a stored double.
        if (ProjectFundingLedger.DescribeUnsupportedGoalUnit(declaration.FundingGoalUnit) is { } badUnit)
            return badUnit;

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
                ProjectStakePartitions.WorkerAllowance,
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
            // A backlog reservation names the allowance it is set aside from, but it commits
            // NOTHING on the node: there is no person to hold the units for yet. It therefore
            // carries no hold id, and every payout path refuses on exactly that (see
            // ApproveWorkCompletionAsync and DistributeAsync).
            reservePartition = ContributorPartitionName,
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
        CancellationToken ct = default)
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
                ProjectStakePartitions.SponsorAllowance,
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

    /// <summary>
    /// Hands an UNHELD reservation to a named recipient. This is the pre-hold path and it is
    /// deliberately narrow: an allocation the node is actually holding units for may only leave
    /// RESERVED through <see cref="ApproveWorkCompletionAsync"/>, because those units are
    /// committed on the node's reserve ledger and crediting a balance here would show a person
    /// a holding the node has not released to them.
    /// </summary>
    public async Task<Result<TokenAllocationDto>> DistributeAsync(
        string allocationId,
        string recipientUserId,
        CancellationToken ct = default)
    {
        var allocation = await _allocationRepo.GetByIdAsync(allocationId, ct);
        if (allocation == null)
            return Result<TokenAllocationDto>.NotFound($"Allocation {allocationId} not found");

        if (allocation.IsNodeHeld)
        {
            return Result<TokenAllocationDto>.Conflict(
                "These units are held for this worker on the shared node. The project owner approves the "
                + "completed work to release them; they cannot be handed over directly.");
        }

        // The recipient of an allocation that already names one is that person, never whoever
        // the caller nominates. A caller-supplied recipient on an owned resource is the IDOR
        // shape this codebase refuses everywhere else.
        if (!string.IsNullOrWhiteSpace(allocation.recipientUserId)
            && !string.Equals(allocation.recipientUserId, recipientUserId, StringComparison.Ordinal))
        {
            return Result<TokenAllocationDto>.Forbidden(
                "This allocation is already set aside for someone else and cannot be redirected.");
        }

        if (allocation.status != AllocationStatus.RESERVED)
            return Result<TokenAllocationDto>.ValidationError(
                $"Only RESERVED allocations can be distributed. Current status: {allocation.status}");

        var config = await _configRepo.GetByIdAsync(allocation.projectTokenConfigId, ct);
        if (config == null)
            return Result<TokenAllocationDto>.NotFound(
                $"Project token config {allocation.projectTokenConfigId} not found");

        // The gate says whether this holder class may EVER move units and the worker's own window
        // says whether they may move them YET. This path hands over a reservation the node holds
        // nothing for, so it opens no window - but it asks the SAME rule every other seam asks,
        // because three copies of one predicate is how they come to disagree.
        allocation.recipientUserId = recipientUserId;
        allocation.status = AllocationStatus.DISTRIBUTED;
        allocation.isLiquid = UnitWindow.IsAwardLiquid(
            allocation, config.gateStatus, DateTimeOffset.UtcNow);
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


    // =====================================================================
    // Worker awards (W7.2): the two holds behind "you will be paid for this"
    //
    // Accepting a bid RESERVES the award as a hold on the node's reserve ledger, drawn from
    // the project's declared contributor allowance. Approving the completed work CONTINUES
    // that hold into a second one — the worker's own window — rather than releasing it, so
    // the units are theirs and restricted at the same time.
    //
    // The oversubscription decision belongs to the node, not to this database: sponsor
    // exchanges and worker holds draw the same partitions, so an allowance policed only here
    // can be overdrawn by the other caller. The local check below is a courtesy that keeps
    // ArdaNova from promising units the node would refuse; it is never the control.
    // =====================================================================

    /// <inheritdoc/>
    public async Task<Result<ProjectAwardReservation>> ReserveWorkAwardAsync(
        ProjectAwardReservationRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            return Result<ProjectAwardReservation>.ValidationError("An award reservation needs a request.");

        if (_holds is null)
            return Result<ProjectAwardReservation>.Failure(HoldGatewayUnwiredError);

        var config = await _configRepo.GetByIdAsync(request.ProjectTokenConfigId, ct);
        if (config is null)
        {
            return Result<ProjectAwardReservation>.NotFound(
                $"Project token config {request.ProjectTokenConfigId} not found");
        }

        if (DescribeUnfundable(config) is { } unfundable)
            return Result<ProjectAwardReservation>.ValidationError(unfundable);

        if (request.RecipientAvatarId == Guid.Empty)
        {
            return Result<ProjectAwardReservation>.Conflict(
                "This worker's account could not be identified on the shared node, so nothing was reserved.");
        }

        if (!TryDescribeWholeUnits(request.AwardAmount, config, out var wholeUnits, out var amountRefusal))
            return Result<ProjectAwardReservation>.ValidationError(amountRefusal);

        // Idempotent on the hold id: a redelivered acceptance resumes the same reservation
        // instead of drawing the allowance twice.
        var existing = await _allocationRepo.FindOneAsync(
            allocation => allocation.holdId == request.HoldId, ct);
        if (existing is not null)
        {
            return string.Equals(existing.projectTokenConfigId, config.id, StringComparison.Ordinal)
                ? Result<ProjectAwardReservation>.Success(DescribeReservation(existing))
                : Result<ProjectAwardReservation>.Conflict(
                    "This award reference already belongs to a different project's stake.");
        }

        if (DescribePartitionOversubscription(
                ProjectStakePartitions.WorkerAllowance,
                config.contributorPartitionSupply,
                config.contributorSupply,
                wholeUnits) is { } contributorRefusal)
        {
            return Result<ProjectAwardReservation>.ValidationError(contributorRefusal);
        }

        var availableSupply = config.totalSupply - config.contributorSupply - config.investorSupply
            - config.founderSupply - config.burnedSupply;
        if (wholeUnits > availableSupply)
        {
            return Result<ProjectAwardReservation>.ValidationError(
                $"There are only {Math.Max(availableSupply, 0)} units of this project's stake left to commit, "
                + $"and this award needs {wholeUnits}.");
        }

        var baseUnits = BaseUnitsOf(wholeUnits);
        var chain = string.IsNullOrWhiteSpace(config.assetChain) ? _chainType : config.assetChain!;

        // AutoRelease is FALSE on purpose. The node's hosted sweep releases any hold that is
        // auto-releasing and has no window, so an escrow hold that asked to be swept would pay
        // the worker before the owner ever approved the work.
        var opened = await _holds.OpenAsync(
            new AzoaHoldOpenRequest(
                new AzoaHoldReference(chain, request.HoldId),
                config.assetId!,
                baseUnits.ToString(),
                ContributorPartitionName,
                request.RecipientAvatarId,
                ReleaseNotBefore: null,
                AutoRelease: false,
                Purpose: ProjectAwardHold.EscrowPurpose),
            ct);

        if (opened.IsFailure || opened.Value is null)
        {
            _logger.LogWarning(
                "Award reservation refused for config {ConfigId} hold {HoldId}: {Reason}",
                config.id, request.HoldId, opened.Error);
            return PropagateHoldFailure<ProjectAwardReservation>(
                opened,
                "The units for this award could not be set aside on the shared node.");
        }

        var hold = opened.Value;
        if (!IsCommitted(hold.State))
        {
            return Result<ProjectAwardReservation>.Failure(
                "The units for this award were not committed on the shared node, so the assignment was not funded.");
        }

        var now = DateTime.UtcNow;
        var reservation = new TokenAllocation
        {
            id = Guid.NewGuid().ToString(),
            projectTokenConfigId = config.id,
            pbiId = null,
            // Set at acceptance, not at payout: the person the units are held for is decided
            // by the agreement and is never a caller-supplied field on a later call.
            recipientUserId = request.RecipientUserId,
            equityPercentage = config.totalSupply > 0
                ? (double)wholeUnits / config.totalSupply * 100
                : 0,
            tokenAmount = wholeUnits,
            status = AllocationStatus.RESERVED,
            holderClass = TokenHolderClass.CONTRIBUTOR,
            isLiquid = false,
            taskCommerceAgreementId = request.AgreementId,
            holdChain = hold.ChainType,
            reservePartition = ContributorPartitionName,
            holdId = hold.HoldId,
            holdState = hold.State,
            holdSyncedAt = now,
            createdAt = now,
            updatedAt = now,
        };

        await _allocationRepo.AddAsync(reservation, ct);

        config.contributorSupply += wholeUnits;
        config.allocatedSupply += wholeUnits;
        config.updatedAt = now;
        await _configRepo.UpdateAsync(config, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Reserved {Units} units of config {ConfigId} for agreement {AgreementId} under hold {HoldId}",
            wholeUnits, config.id, request.AgreementId, hold.HoldId);

        return Result<ProjectAwardReservation>.Success(DescribeReservation(reservation));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Approval settles EVERY leg of the assignment, not just the one named. An accepted bid
    /// that declared a guild share is held in two reservations against the same agreement, and
    /// the rule that a leg's recipient must equal the agreement's contributor made the guild's
    /// leg unsettleable - while this same call marked the agreement SETTLED and the task
    /// RELEASED, so its units were drawn from the project and stranded on the node with no
    /// settle and no refund that could reach them. The leg set IS the beneficiary set: every
    /// row was written server-side at acceptance from the bid, never from a caller's field.
    /// The worker's own leg must be among them, or the set is not this assignment's.
    /// </remarks>
    public async Task<Result<TokenAllocationDto>> ApproveWorkCompletionAsync(
        string allocationId,
        string actorUserId,
        CancellationToken ct = default)
    {
        if (_projectRepo is null || _agreementRepo is null || _taskRepo is null || _ownerAccounts is null)
            return Result<TokenAllocationDto>.Failure(AwardUnwiredError);

        if (_holds is null)
            return Result<TokenAllocationDto>.Failure(HoldGatewayUnwiredError);

        var allocation = await _allocationRepo.GetByIdAsync(allocationId, ct);
        if (allocation is null)
            return Result<TokenAllocationDto>.NotFound($"Allocation {allocationId} not found");

        // A reservation the node never committed backs nothing. Paying one out would credit a
        // balance against units no partition was ever drawn for.
        if (!allocation.IsNodeHeld)
        {
            return Result<TokenAllocationDto>.ValidationError(
                "These units were never set aside on the shared node, so there is nothing to hand over.");
        }

        var config = await _configRepo.GetByIdAsync(allocation.projectTokenConfigId, ct);
        if (config is null)
        {
            return Result<TokenAllocationDto>.NotFound(
                $"Project token config {allocation.projectTokenConfigId} not found");
        }

        var project = await _projectRepo.GetByIdAsync(config.projectId, ct);
        if (project is null)
            return Result<TokenAllocationDto>.NotFound($"Project {config.projectId} not found");

        // Approving completed work spends the project's stake, so it is the project owner's
        // decision alone. The owner comes from the caller's verified identity.
        if (!string.Equals(project.createdById, actorUserId, StringComparison.Ordinal))
        {
            return Result<TokenAllocationDto>.Forbidden(
                "Only the person who created this project can approve completed work on it.");
        }

        var agreement = string.IsNullOrWhiteSpace(allocation.taskCommerceAgreementId)
            ? null
            : await _agreementRepo.GetByIdAsync(allocation.taskCommerceAgreementId!, ct);
        if (agreement is null)
        {
            return Result<TokenAllocationDto>.Conflict(
                "This award is not linked to an accepted assignment, so there is no completed work to approve.");
        }

        if (!string.Equals(agreement.projectId, project.id, StringComparison.Ordinal))
        {
            return Result<TokenAllocationDto>.Conflict(
                "This award and the assignment it belongs to disagree about the project or the worker.");
        }

        var legs = (await _allocationRepo.FindAsync(
                candidate => candidate.taskCommerceAgreementId == agreement.id, ct))
            .Where(candidate => string.Equals(
                candidate.projectTokenConfigId, config.id, StringComparison.Ordinal))
            .OrderBy(candidate => candidate.id, StringComparer.Ordinal)
            .ToList();

        // The worker's own share is what makes this set the assignment's. A set without it is
        // either a different project's stake or a half-written acceptance; neither may settle.
        if (!legs.Any(leg => string.Equals(
                leg.recipientUserId, agreement.contributorUserId, StringComparison.Ordinal)))
        {
            return Result<TokenAllocationDto>.Conflict(
                "This award and the assignment it belongs to disagree about the project or the worker.");
        }

        foreach (var leg in legs)
        {
            var refusal = await SettleAwardLegAsync(leg, config, actorUserId, ct);
            if (refusal is not null)
                return refusal;
        }

        var now = DateTime.UtcNow;

        // Only once EVERY leg is the recipient's. A partial settlement leaves the agreement and
        // the task's escrow where they were, which is exactly what is true: the legs already
        // continued are idempotent, so a retry finishes the rest.
        if (agreement.status != TaskCommerceAgreementStatus.SETTLED)
        {
            agreement.status = TaskCommerceAgreementStatus.SETTLED;
            agreement.releaseAuthorizedAt ??= now;
            agreement.settledAt = now;
            agreement.updatedAt = now;
            await _agreementRepo.UpdateAsync(agreement, ct);

            var task = await _taskRepo.GetByIdAsync(agreement.taskId, ct);
            if (task is not null)
            {
                task.escrowStatus = EscrowStatus.RELEASED;
                task.updatedAt = now;
                await _taskRepo.UpdateAsync(task, ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return Result<TokenAllocationDto>.Success(_mapper.Map<TokenAllocationDto>(allocation));
    }

    /// <summary>
    /// Moves ONE beneficiary's share into their own window on the node. Returns null when the
    /// leg is settled (or already was), and the refusal to carry out otherwise.
    /// </summary>
    /// <remarks>
    /// Every field the recipient is decided by comes off the allocation row, which was written
    /// at acceptance from the bid. Crediting the agreement's contributor here instead would pay
    /// the worker for the guild's share as well as their own.
    /// </remarks>
    private async Task<Result<TokenAllocationDto>?> SettleAwardLegAsync(
        TokenAllocation leg,
        ProjectTokenConfig config,
        string actorUserId,
        CancellationToken ct)
    {
        // Idempotent: the window already exists, so a redelivered approval leaves it alone
        // rather than opening a second one.
        if (leg.IsAwaitingWorkerWindow)
            return null;

        if (!leg.IsNodeHeld)
        {
            return Result<TokenAllocationDto>.ValidationError(
                "These units were never set aside on the shared node, so there is nothing to hand over.");
        }

        if (leg.status != AllocationStatus.RESERVED)
        {
            return Result<TokenAllocationDto>.ValidationError(
                $"Only a reserved award can be approved. This one is {leg.status}.");
        }

        // A leg with no recipient names nobody to hand units to. Continuing would ask the node
        // to hold them for an empty id.
        if (string.IsNullOrWhiteSpace(leg.recipientUserId))
        {
            return Result<TokenAllocationDto>.Conflict(
                "This award has no recipient recorded, so there is nobody to hand it to.");
        }

        var recipientAvatar = await ResolveReadyAvatarAsync(leg.recipientUserId!, ct);
        if (recipientAvatar.IsFailure)
            return ValueEligibilityFailure.Propagate<Guid, TokenAllocationDto>(recipientAvatar);

        var chain = string.IsNullOrWhiteSpace(leg.holdChain) ? _chainType : leg.holdChain!;
        var reference = new AzoaHoldReference(chain, leg.holdId!);
        var workerHoldId = ProjectAwardHold.WorkerWindowIdFor(leg.holdId!);
        // F31: the node keys the continuation on the WHOLE intent, so every retry must send the
        // same instant. See Services/Implementations/AGENTS.md §award-window-instant.
        var releasesAt = await ResolveWorkerWindowAsync(leg, chain, workerHoldId, ct);

        // A plain release cannot express "yours, and restricted until the window elapses", so
        // the escrow hold is CONTINUED into the recipient's own hold. Nothing leaves the node's
        // holding account here; only who it is held for and until when change.
        var continued = await _holds!.ReleaseIntoHoldAsync(
            new AzoaHoldChainRequest(
                reference,
                workerHoldId,
                recipientAvatar.Value,
                ReleaseNotBefore: releasesAt,
                // The node's direct hold door refuses auto-release without a release mandate
                // (see Infrastructure/Azoa/AGENTS.md §Hold commands); the window is enforced here.
                AutoRelease: false,
                Purpose: ProjectAwardHold.WorkerWindowPurpose),
            ct);

        if (continued.IsFailure || continued.Value is null)
        {
            _logger.LogWarning(
                "Award approval refused for allocation {AllocationId} hold {HoldId}: {Reason}",
                leg.id, leg.holdId, continued.Error);
            return PropagateHoldFailure<TokenAllocationDto>(
                continued,
                "The approved award could not be moved into this worker's window on the shared node.");
        }

        var parent = continued.Value;
        var childHoldId = string.IsNullOrWhiteSpace(parent.ChildHoldId) ? workerHoldId : parent.ChildHoldId!;
        var childState = await ReadHoldStateAsync(chain, childHoldId, ct);

        var now = DateTime.UtcNow;
        leg.status = AllocationStatus.DISTRIBUTED;
        leg.holdState = parent.State;
        leg.workerHoldId = childHoldId;
        leg.workerHoldState = childState;
        leg.workerHoldReleasesAt = releasesAt.UtcDateTime;
        // Restricted until the window elapses, whatever the gate says. Stamped AFTER the window
        // columns above, because it is computed FROM them by the one rule every other seam reads;
        // this column is a record of the answer at approval, never the authority - the window ends
        // on a date nothing revisits, so the seams re-read the award rather than trusting it.
        leg.isLiquid = UnitWindow.IsAwardLiquid(leg, config.gateStatus, DateTimeOffset.UtcNow);
        leg.holdSyncedAt = now;
        leg.distributedAt = now;
        leg.updatedAt = now;
        await _allocationRepo.UpdateAsync(leg, ct);

        config.distributedSupply += leg.tokenAmount;
        config.updatedAt = now;
        await _configRepo.UpdateAsync(config, ct);

        var credited = await _tokenBalanceService.CreditAsync(
            leg.recipientUserId!,
            leg.projectTokenConfigId,
            leg.tokenAmount,
            leg.holderClass,
            ct);

        if (credited.IsFailure)
        {
            _logger.LogError(
                "Award {AllocationId} moved into hold {WorkerHoldId} but the balance read-model was not credited: {Error}",
                leg.id, childHoldId, credited.Error);
            return Result<TokenAllocationDto>.Failure(
                "The award is now held for this worker, but their holdings could not be refreshed. "
                + "Nothing is lost; retry to bring the figures up to date.");
        }

        _logger.LogInformation(
            "Award {AllocationId} approved by {ActorId}; held for {RecipientId} until {ReleasesAt:u}",
            leg.id, actorUserId, leg.recipientUserId, releasesAt);

        return null;
    }

    /// <summary>
    /// The worker window's release instant: the one the node already committed for this window,
    /// else the leg's pinned instant, deciding and SAVING it (whole seconds) before first use.
    /// </summary>
    private async Task<DateTimeOffset> ResolveWorkerWindowAsync(
        TokenAllocation leg,
        string chain,
        string workerHoldId,
        CancellationToken ct)
    {
        // The node is the authority once it has continued the hold: a local rollback after its
        // commit (or a lost response) must replay ITS instant, to the tick, or be refused.
        var committed = await _holds!.GetAsync(new AzoaHoldReference(chain, workerHoldId), ct);
        if (committed is { IsSuccess: true, Value: { ReleaseNotBefore: { } adopted } window }
            && string.Equals(window.HoldId, workerHoldId, StringComparison.Ordinal))
        {
            return adopted;
        }

        if (leg.workerHoldReleasesAt is null)
        {
            // Whole seconds: the column keeps milliseconds, so a finer instant would come back
            // from the database different from the one first sent.
            var decided = DateTime.UtcNow.AddDays(_awardWindowDays);
            leg.workerHoldReleasesAt = new DateTime(
                decided.Ticks - decided.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
            await _allocationRepo.UpdateAsync(leg, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return new DateTimeOffset(DateTime.SpecifyKind(leg.workerHoldReleasesAt.Value, DateTimeKind.Utc));
    }

    /// <summary>The node's live state for one hold, or null when it could not be read.</summary>
    private async Task<string?> ReadHoldStateAsync(string chain, string holdId, CancellationToken ct)
    {
        var read = await _holds!.GetAsync(new AzoaHoldReference(chain, holdId), ct);
        if (read.IsSuccess && read.Value is not null)
            return read.Value.State;

        // A mirror that could not be read stays empty rather than guessing: a state written
        // from nothing would look exactly like a state the node confirmed.
        _logger.LogWarning("Hold {HoldId} state could not be read back: {Reason}", holdId, read.Error);
        return null;
    }

    /// <summary>
    /// The worker's avatar on the node, refused unless the SHARED gate says their account is
    /// value-ready (D46). This service used to keep its own four-flag predicate, which drifted
    /// from the gate in both directions: it never required the avatar and managed-account
    /// evidence the gate requires, and it answered an unreadable node with Forbidden carrying
    /// the raw provider diagnostic - telling a worker they are not allowed, in the node's own
    /// words, because the platform was down.
    /// </summary>
    private async Task<Result<Guid>> ResolveReadyAvatarAsync(string userId, CancellationToken ct)
    {
        var readiness = await _valueEligibility.RequireReadyAsync(userId, AwardHandoverAction, ct);
        if (!readiness.IsSuccess)
            return ValueEligibilityFailure.Propagate<AzoaCustodialAccountStatusDto, Guid>(readiness);

        return Guid.TryParse(readiness.Value?.AvatarId, out var avatarId) && avatarId != Guid.Empty
            ? Result<Guid>.Success(avatarId)
            : Result<Guid>.Conflict(ValueEligibilityMessages.SubjectUnknown);
    }

    /// <summary>Whole units of stake this award is worth, exact at the stake's own scale.</summary>
    private static bool TryDescribeWholeUnits(
        decimal awardAmount,
        ProjectTokenConfig config,
        out int wholeUnits,
        out string refusal)
    {
        wholeUnits = 0;
        refusal = string.Empty;

        if (awardAmount <= 0)
        {
            refusal = "An award must be worth at least one unit of this project's stake.";
            return false;
        }

        if (decimal.Truncate(awardAmount) != awardAmount)
        {
            // The declared allowances are counted in whole units, so a part-unit award would
            // be committed at one size on the node and counted at another here.
            refusal = "An award must be a whole number of units of this project's stake.";
            return false;
        }

        if (awardAmount > int.MaxValue)
        {
            refusal = "This award is larger than this platform can issue.";
            return false;
        }

        var scale = config.assetScale ?? PlatformAssetScale;
        if (!FixedScaleAmount.TryFromPositiveDecimal(awardAmount, scale, out _))
        {
            refusal = "This award cannot be expressed exactly in this project's units.";
            return false;
        }

        wholeUnits = (int)awardAmount;
        return true;
    }

    /// <summary>True only for a state in which the node is actually holding the units.</summary>
    private static bool IsCommitted(string? state)
        => string.Equals(state, AzoaHoldStates.Held, StringComparison.OrdinalIgnoreCase);

    /// <summary>The reservation as its caller reads it back: id, hold, state, units.</summary>
    private static ProjectAwardReservation DescribeReservation(TokenAllocation allocation)
        => new(
            allocation.id,
            allocation.holdId ?? string.Empty,
            allocation.holdState ?? AzoaHoldStates.Unknown,
            allocation.tokenAmount);

    /// <summary>Carries a node refusal to the caller's own Result without flattening its type.</summary>
    private static Result<TOut> PropagateHoldFailure<TOut>(Result<AzoaHold> source, string fallback)
    {
        var error = source.Error ?? fallback;
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

        // Once the escrow hold has been continued into the worker's own window the units are
        // theirs, restricted rather than reversible. Revoking here would free the allowance in
        // this database while the node still holds the commitment for that person.
        if (allocation.IsAwaitingWorkerWindow)
        {
            return Result<TokenAllocationDto>.Conflict(
                "This award is already being held for the worker and cannot be taken back.");
        }

        var config = await _configRepo.GetByIdAsync(allocation.projectTokenConfigId, ct);
        if (config == null)
            return Result<TokenAllocationDto>.NotFound(
                $"Project token config {allocation.projectTokenConfigId} not found");

        // The node drew this commitment out of a named allowance, so the node has to give it
        // back. Freeing only the local counter would leave the allowance permanently short by
        // the amount of every revoked award.
        if (allocation.IsNodeHeld)
        {
            if (_holds is null)
                return Result<TokenAllocationDto>.Failure(HoldGatewayUnwiredError);

            var chain = string.IsNullOrWhiteSpace(allocation.holdChain) ? _chainType : allocation.holdChain!;
            var refunded = await _holds.RefundAsync(
                new AzoaHoldReference(chain, allocation.holdId!),
                "the project owner withdrew this award",
                ct);

            if (refunded.IsFailure || refunded.Value is null)
            {
                _logger.LogWarning(
                    "Award {AllocationId} could not be given back on hold {HoldId}: {Reason}",
                    allocation.id, allocation.holdId, refunded.Error);
                return PropagateHoldFailure<TokenAllocationDto>(
                    refunded,
                    "The units set aside for this award could not be released back to the project.");
            }

            allocation.holdState = refunded.Value.State;
            allocation.holdSyncedAt = DateTime.UtcNow;
        }

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
/// <para>
/// It carries the node-side half of a project's stake end to end: the funding setup that
/// creates it, and the award holds that pay a worker out of it. Both halves mutate the same
/// supply counters, so keeping them on one service keeps a single writer for each number.
/// </para>
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

    /// <summary>
    /// Sets a worker's award aside as a HOLD on the node, drawn from this project's declared
    /// contributor allowance. Idempotent on <see cref="ProjectAwardReservationRequest.HoldId"/>:
    /// a redelivered acceptance resumes the same reservation and never draws the allowance
    /// twice. The node is the authority on whether the allowance can still afford it; this
    /// method never invents supply it could not commit.
    /// </summary>
    Task<Result<ProjectAwardReservation>> ReserveWorkAwardAsync(
        ProjectAwardReservationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// The project owner approves completed work: the escrow hold is CONTINUED into the
    /// worker's own window (release-into-hold), the agreement settles, and the award becomes
    /// the worker's — restricted until the window elapses. <paramref name="actorUserId"/> is
    /// the caller's verified identity and must be the project's creator; the person paid comes
    /// from the agreement, never from the request.
    /// </summary>
    Task<Result<TokenAllocationDto>> ApproveWorkCompletionAsync(
        string allocationId,
        string actorUserId,
        CancellationToken ct = default);
}

/// <summary>What one accepted assignment needs in order to reserve its award on the node.</summary>
/// <param name="ProjectTokenConfigId">The project's stake.</param>
/// <param name="AgreementId">The commerce agreement this award was accepted under.</param>
/// <param name="HoldId">The caller's own hold id — the idempotency anchor for the whole award.</param>
/// <param name="AwardAmount">Whole units of stake, as the accepted bid expressed them.</param>
/// <param name="RecipientUserId">The ArdaNova person the units are held for.</param>
/// <param name="RecipientAvatarId">That person's account on the node.</param>
public sealed record ProjectAwardReservationRequest(
    string ProjectTokenConfigId,
    string AgreementId,
    string HoldId,
    decimal AwardAmount,
    string RecipientUserId,
    Guid RecipientAvatarId);

/// <summary>What the node committed, echoed back for the caller's own records.</summary>
public sealed record ProjectAwardReservation(
    string AllocationId,
    string HoldId,
    string HoldState,
    int WholeUnits);

/// <summary>
/// How the two holds behind one worker award are named, and why each of them exists.
/// </summary>
/// <remarks>
/// Both ids are derived from the acceptance, so a redelivered acceptance or approval addresses
/// the hold it already opened rather than opening a second one, and neither is ever a value a
/// caller supplies.
/// </remarks>
public static class ProjectAwardHold
{
    /// <summary>Longest id this transport will put in a URL segment unescaped.</summary>
    private const int MaximumTransportIdLength = 128;

    /// <summary>Longest id the escrow half may take, so its continuation still fits.</summary>
    private const int MaximumEscrowIdLength = 100;

    /// <summary>Why the first hold exists: the units are committed but not yet anyone's.</summary>
    public const string EscrowPurpose = "Stake set aside for accepted project work";

    /// <summary>
    /// Why the second hold exists: the award IS the worker's, and restricted until it becomes
    /// liquid, so the taxable event falls when they can realise it rather than when it was
    /// granted. It is not a chargeback window — these units came out of the platform reserve
    /// and were never bought with a card, so there is nothing to reverse.
    /// </summary>
    public const string WorkerWindowPurpose = "Awarded stake, restricted until it becomes liquid";

    /// <summary>The escrow hold's id. The accepted bid is the idempotency anchor.</summary>
    public static string EscrowIdFor(string bidId) => "award-" + Sanitise(bidId);

    /// <summary>The worker's window, continued out of the escrow hold it names.</summary>
    public static string WorkerWindowIdFor(string escrowHoldId) => escrowHoldId + "-window";

    /// <summary>True when an id survives the transport's own path-segment alphabet unchanged.</summary>
    public static bool IsAddressable(string? holdId)
        => !string.IsNullOrWhiteSpace(holdId)
            && holdId.Length <= MaximumTransportIdLength
            && holdId.All(character =>
                char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');

    private static string Sanitise(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        var cleaned = new string(trimmed
            .Select(character =>
                char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.'
                    ? character
                    : '-')
            .ToArray());
        return cleaned.Length > MaximumEscrowIdLength ? cleaned[..MaximumEscrowIdLength] : cleaned;
    }
}

/// <summary>
/// The node's HOLD surface (D14 / W3.4) as ArdaNova's value paths need it: commit units out of
/// a declared allowance, continue a commitment into a second window, or give it back.
/// </summary>
/// <remarks>
/// Zero implementations is a supported state: a deployment that has not wired the adapter still
/// boots, and every caller refuses by name rather than treating an unheld award as funded.
/// </remarks>
public interface IAzoaHoldGateway
{
    /// <summary>Opens a hold, or returns the one this hold id already opened. Idempotent.</summary>
    Task<Result<AzoaHold>> OpenAsync(AzoaHoldOpenRequest request, CancellationToken ct = default);

    /// <summary>Continues a hold as a second hold over the same units. Idempotent on the next id.</summary>
    Task<Result<AzoaHold>> ReleaseIntoHoldAsync(AzoaHoldChainRequest request, CancellationToken ct = default);

    /// <summary>Gives a hold back: the commitment returns to the allowance it was drawn from.</summary>
    Task<Result<AzoaHold>> RefundAsync(
        AzoaHoldReference reference,
        string? reason,
        CancellationToken ct = default);

    /// <summary>Reads one hold as the node currently records it.</summary>
    Task<Result<AzoaHold>> GetAsync(AzoaHoldReference reference, CancellationToken ct = default);
}

/// <summary>Chain plus the caller's own hold id — together they address exactly one hold.</summary>
public sealed record AzoaHoldReference(string ChainType, string HoldId);

/// <summary>What the node needs in order to commit units out of a declared allowance.</summary>
public sealed record AzoaHoldOpenRequest(
    AzoaHoldReference Reference,
    string AssetId,
    string AmountBaseUnits,
    string ReservePartition,
    Guid BeneficiaryAvatarId,
    DateTimeOffset? ReleaseNotBefore,
    bool AutoRelease,
    string? Purpose);

/// <summary>What the node needs in order to continue one hold as another.</summary>
public sealed record AzoaHoldChainRequest(
    AzoaHoldReference Reference,
    string NextHoldId,
    Guid NextBeneficiaryAvatarId,
    DateTimeOffset? ReleaseNotBefore,
    bool AutoRelease,
    string? Purpose);

/// <summary>
/// One hold as the node reports it. It deliberately carries no address and no rail reference:
/// the person a hold is for needs to know how much is theirs and when, not which account it
/// is parked in.
/// </summary>
public sealed record AzoaHold(
    string HoldId,
    string ChainType,
    string AssetId,
    string AmountBaseUnits,
    string ReservePartition,
    string? BeneficiaryReference,
    string State,
    DateTimeOffset? ReleaseNotBefore,
    bool AutoRelease,
    string? ParentHoldId,
    string? ChildHoldId);

/// <summary>The node's hold lifecycle, mirrored as text because it crosses a wire.</summary>
public static class AzoaHoldStates
{
    /// <summary>The row is claimed but the commitment has not been drawn yet.</summary>
    public const string Pending = "Pending";

    /// <summary>Units are committed and sitting in the node's holding account.</summary>
    public const string Held = "Held";

    /// <summary>The commitment is drawn and the outbound move is in flight.</summary>
    public const string Releasing = "Releasing";

    /// <summary>The beneficiary has the units. Terminal.</summary>
    public const string Released = "Released";

    /// <summary>The commitment went back to its allowance; nothing moved. Terminal.</summary>
    public const string Refunded = "Refunded";

    /// <summary>The hold was continued as a second hold over the same units. Terminal.</summary>
    public const string ReleasedIntoHold = "ReleasedIntoHold";

    /// <summary>The node reported no state this deployment recognises.</summary>
    public const string Unknown = "Unknown";
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

    /// <summary>What the project is asking for, in <see cref="FundingGoalUnit"/>.</summary>
    public required double FundingGoal { get; init; }

    /// <summary>
    /// The unit <see cref="FundingGoal"/> is denominated in. Required, and refused unless it is
    /// the unit the funding columns are actually kept in
    /// (<c>ProjectFundingUnits.Ledger</c>, dollars). The ask used to arrive with no unit at all
    /// while the client sent platform units, so it was stored beside a dollar-denominated raise
    /// and divided into a dollar-denominated price. A caller that still sends the old unit now
    /// fails at the door instead of mispricing every sponsorship of the project.
    /// </summary>
    public required string FundingGoalUnit { get; init; }

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
