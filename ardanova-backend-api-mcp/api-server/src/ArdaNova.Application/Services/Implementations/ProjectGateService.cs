using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.Policies;
using AutoMapper;

namespace ArdaNova.Application.Services.Implementations
{
    /// <summary>
    /// Moves a project through its funding gates and restates who may move units afterwards.
    /// </summary>
    /// <remarks>
    /// A gate decides whether a holder class may EVER move units; it does not decide whether one
    /// person's award may move YET. Clearing a gate therefore RE-EVALUATES each row through
    /// <see cref="UnitWindow"/> rather than setting it liquid: a blanket unlock here used to
    /// override the worker's own 90-day window written at approval, so the platform told a worker
    /// their award was held until a date and then read it as liquid the same day.
    /// </remarks>
    public class ProjectGateService : IProjectGateService
    {
        private readonly IRepository<ProjectTokenConfig> _projectTokenConfigRepository;
        private readonly IRepository<TokenAllocation> _tokenAllocationRepository;
        private readonly IRepository<TokenBalance> _tokenBalanceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IProjectTokenService _projectTokenService;
        private readonly ITreasuryService _treasuryService;

        public ProjectGateService(
            IRepository<ProjectTokenConfig> projectTokenConfigRepository,
            IRepository<TokenAllocation> tokenAllocationRepository,
            IRepository<TokenBalance> tokenBalanceRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IProjectTokenService projectTokenService,
            ITreasuryService treasuryService)
        {
            _projectTokenConfigRepository = projectTokenConfigRepository;
            _tokenAllocationRepository = tokenAllocationRepository;
            _tokenBalanceRepository = tokenBalanceRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _projectTokenService = projectTokenService;
            _treasuryService = treasuryService;
        }

        public async Task<Result<GateTransitionResultDto>> EvaluateGate1Async(string projectTokenConfigId, CancellationToken ct = default)
        {
            // 1. Get config and validate gate status
            var config = await _projectTokenConfigRepository.GetByIdAsync(projectTokenConfigId, ct);
            if (config == null)
            {
                return Result<GateTransitionResultDto>.Failure("ProjectTokenConfig not found.");
            }

            if (config.gateStatus != ProjectGateStatus.FUNDING)
            {
                return Result<GateTransitionResultDto>.Failure($"Invalid gate status. Expected FUNDING, got {config.gateStatus}.");
            }

            // 2. Is the primary raise finished? Both figures are dollars, and the ONE reader that
            // says so is shared with the pricing path - a hand-rolled `raised < goal` here is how
            // the two columns drifted into different units in the first place.
            //
            // The ask being met is not the only way a raise finishes. Each sponsorship's dollar
            // reference is floored to the cent, so a project can sell every unit it offered and
            // still sit a few cents under its ask; with the ask as the sole condition those cents
            // froze the gate forever and with it every worker's stake. `RaiseIsComplete` clears on
            // either - and refuses to treat a project that declared NO sponsor stake, or took no
            // money for the units it did commit, as sold out.
            if (!ProjectRaiseOffering.RaiseIsComplete(config))
            {
                var noTransitionResult = new GateTransitionResultDto
                {
                    Transitioned = false,
                    PreviousStatus = ProjectGateStatus.FUNDING,
                    NewStatus = ProjectGateStatus.FUNDING,
                    TokensUnlocked = 0
                };
                return Result<GateTransitionResultDto>.Success(noTransitionResult);
            }

            // 3. Transition gate status
            config.gateStatus = ProjectGateStatus.ACTIVE;
            config.gate1ClearedAt = DateTime.UtcNow;
            config.updatedAt = DateTime.UtcNow;
            await _projectTokenConfigRepository.UpdateAsync(config, ct);

            // 4. Re-evaluate TokenAllocations for CONTRIBUTOR holder class
            Expression<Func<TokenAllocation, bool>> allocationPredicate = ta =>
                ta.projectTokenConfigId == projectTokenConfigId &&
                ta.holderClass == TokenHolderClass.CONTRIBUTOR;

            var allocations = await _tokenAllocationRepository.FindAsync(allocationPredicate, ct);
            var allocationsList = allocations.ToList();
            var now = DateTimeOffset.UtcNow;

            foreach (var allocation in allocationsList)
            {
                allocation.isLiquid = UnitWindow.IsAwardLiquid(allocation, config.gateStatus, now);
                allocation.updatedAt = DateTime.UtcNow;
                await _tokenAllocationRepository.UpdateAsync(allocation, ct);
            }

            // 5. Re-evaluate TokenBalances for CONTRIBUTOR holder class
            Expression<Func<TokenBalance, bool>> balancePredicate = tb =>
                tb.projectTokenConfigId == projectTokenConfigId &&
                tb.holderClass == TokenHolderClass.CONTRIBUTOR;

            var balances = await _tokenBalanceRepository.FindAsync(balancePredicate, ct);
            var balancesList = balances.ToList();

            foreach (var balance in balancesList)
            {
                balance.isLiquid = IsHoldingLiquid(balance, allocationsList, config.gateStatus, now);
                balance.updatedAt = DateTime.UtcNow;
                await _tokenBalanceRepository.UpdateAsync(balance, ct);
            }

            // 6. Count what actually became movable, not what was looked at
            var tokensUnlocked = allocationsList.Where(a => a.isLiquid).Sum(a => a.tokenAmount) +
                                balancesList.Where(b => b.isLiquid).Sum(b => b.balance);

            // 7. Save and return result
            await _unitOfWork.SaveChangesAsync(ct);

            var result = new GateTransitionResultDto
            {
                Transitioned = true,
                PreviousStatus = ProjectGateStatus.FUNDING,
                NewStatus = ProjectGateStatus.ACTIVE,
                TokensUnlocked = tokensUnlocked
            };

            return Result<GateTransitionResultDto>.Success(result);
        }

        public async Task<Result<GateTransitionResultDto>> ClearGate2Async(string projectTokenConfigId, string verifiedByUserId, CancellationToken ct = default)
        {
            // 1. Get config and validate gate status
            var config = await _projectTokenConfigRepository.GetByIdAsync(projectTokenConfigId, ct);
            if (config == null)
            {
                return Result<GateTransitionResultDto>.Failure("ProjectTokenConfig not found.");
            }

            if (config.gateStatus != ProjectGateStatus.ACTIVE)
            {
                return Result<GateTransitionResultDto>.Failure($"Invalid gate status. Expected ACTIVE, got {config.gateStatus}.");
            }

            // 2. Transition gate status
            config.gateStatus = ProjectGateStatus.SUCCEEDED;
            config.gate2ClearedAt = DateTime.UtcNow;
            config.successVerifiedBy = verifiedByUserId;
            config.updatedAt = DateTime.UtcNow;
            await _projectTokenConfigRepository.UpdateAsync(config, ct);

            // 3. Re-evaluate TokenAllocations for INVESTOR and FOUNDER holder classes
            Expression<Func<TokenAllocation, bool>> allocationPredicate = ta =>
                ta.projectTokenConfigId == projectTokenConfigId &&
                (ta.holderClass == TokenHolderClass.INVESTOR || ta.holderClass == TokenHolderClass.FOUNDER);

            var allocations = await _tokenAllocationRepository.FindAsync(allocationPredicate, ct);
            var allocationsList = allocations.ToList();
            var now = DateTimeOffset.UtcNow;

            foreach (var allocation in allocationsList)
            {
                allocation.isLiquid = UnitWindow.IsAwardLiquid(allocation, config.gateStatus, now);
                allocation.updatedAt = DateTime.UtcNow;
                await _tokenAllocationRepository.UpdateAsync(allocation, ct);
            }

            // 4. Re-evaluate TokenBalances for INVESTOR and FOUNDER holder classes
            Expression<Func<TokenBalance, bool>> balancePredicate = tb =>
                tb.projectTokenConfigId == projectTokenConfigId &&
                (tb.holderClass == TokenHolderClass.INVESTOR || tb.holderClass == TokenHolderClass.FOUNDER);

            var balances = await _tokenBalanceRepository.FindAsync(balancePredicate, ct);
            var balancesList = balances.ToList();

            foreach (var balance in balancesList)
            {
                balance.isLiquid = IsHoldingLiquid(balance, allocationsList, config.gateStatus, now);
                balance.updatedAt = DateTime.UtcNow;
                await _tokenBalanceRepository.UpdateAsync(balance, ct);
            }

            // 5. Count what actually became movable, not what was looked at
            var tokensUnlocked = allocationsList.Where(a => a.isLiquid).Sum(a => a.tokenAmount) +
                                balancesList.Where(b => b.isLiquid).Sum(b => b.balance);

            // 6. Save and return result
            await _unitOfWork.SaveChangesAsync(ct);

            var result = new GateTransitionResultDto
            {
                Transitioned = true,
                PreviousStatus = ProjectGateStatus.ACTIVE,
                NewStatus = ProjectGateStatus.SUCCEEDED,
                TokensUnlocked = tokensUnlocked
            };

            return Result<GateTransitionResultDto>.Success(result);
        }

        public async Task<Result<GateTransitionResultDto>> FailProjectAsync(string projectTokenConfigId, string reason, CancellationToken ct = default)
        {
            // 1. Get config and validate gate status
            var config = await _projectTokenConfigRepository.GetByIdAsync(projectTokenConfigId, ct);
            if (config == null)
            {
                return Result<GateTransitionResultDto>.Failure("ProjectTokenConfig not found.");
            }

            if (config.gateStatus != ProjectGateStatus.ACTIVE)
            {
                return Result<GateTransitionResultDto>.Failure($"Invalid gate status. Can only fail projects with ACTIVE status, got {config.gateStatus}.");
            }

            // 2. Transition gate status
            config.gateStatus = ProjectGateStatus.FAILED;
            config.failedAt = DateTime.UtcNow;
            config.updatedAt = DateTime.UtcNow;
            await _projectTokenConfigRepository.UpdateAsync(config, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            // 3. Burn founder tokens
            var burnResult = await _projectTokenService.BurnFounderTokensAsync(projectTokenConfigId);
            if (!burnResult.IsSuccess)
            {
                return Result<GateTransitionResultDto>.Failure($"Failed to burn founder tokens: {burnResult.Error}");
            }

            // 4. Process investor trust protection
            var trustProtectionResult = await _projectTokenService.ProcessInvestorTrustProtectionAsync(projectTokenConfigId);
            if (!trustProtectionResult.IsSuccess)
            {
                return Result<GateTransitionResultDto>.Failure($"Failed to process investor trust protection: {trustProtectionResult.Error}");
            }

            // 5. Save and return result
            await _unitOfWork.SaveChangesAsync(ct);

            var result = new GateTransitionResultDto
            {
                Transitioned = true,
                PreviousStatus = ProjectGateStatus.ACTIVE,
                NewStatus = ProjectGateStatus.FAILED,
                TokensUnlocked = 0,
                TokensBurned = 0,
                TrustProtectionPaid = 0
            };

            return Result<GateTransitionResultDto>.Success(result);
        }

        /// <summary>
        /// Whether a holding may move once a gate has cleared: the gate says ever, the holder's own
        /// awards say yet.
        /// </summary>
        /// <remarks>
        /// The window is read off the awards ALREADY LOADED for this transition rather than
        /// re-queried per holder, so clearing a gate stays one pair of reads however many people
        /// hold the project's units. A holding whose every unit is still inside the window is not
        /// liquid, which is what a blanket unlock here used to overwrite.
        /// </remarks>
        private static bool IsHoldingLiquid(
            TokenBalance holding,
            IReadOnlyCollection<TokenAllocation> awards,
            ProjectGateStatus gateStatus,
            DateTimeOffset now)
        {
            if (holding.holderClass is not { } holderClass
                || !TokenLiquidityPolicy.IsLiquid(holderClass, gateStatus))
            {
                return false;
            }

            var restriction = UnitWindow.Restrict(
                awards.Where(award => award.recipientUserId == holding.userId), now);

            return restriction.RestrictedUnits < holding.balance;
        }

        public async Task<Result<ProjectGateStatusDto>> GetGateStatusAsync(string projectTokenConfigId, CancellationToken ct = default)
        {
            var config = await _projectTokenConfigRepository.GetByIdAsync(projectTokenConfigId, ct);
            if (config == null)
            {
                return Result<ProjectGateStatusDto>.Failure("ProjectTokenConfig not found.");
            }

            var statusDto = new ProjectGateStatusDto
            {
                ProjectTokenConfigId = config.id,
                GateStatus = config.gateStatus,
                FundingGoal = config.fundingGoal,
                FundingRaised = config.fundingRaised,
                Gate1ClearedAt = config.gate1ClearedAt,
                Gate2ClearedAt = config.gate2ClearedAt,
                FailedAt = config.failedAt
            };

            return Result<ProjectGateStatusDto>.Success(statusDto);
        }
    }
}
