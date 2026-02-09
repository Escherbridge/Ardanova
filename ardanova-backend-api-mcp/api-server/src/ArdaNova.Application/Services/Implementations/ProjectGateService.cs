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
using AutoMapper;

namespace ArdaNova.Application.Services.Implementations
{
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

            // 2. Check if funding goal is reached
            if (config.fundingRaised < config.fundingGoal)
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

            // 4. Unlock TokenAllocations for CONTRIBUTOR holder class
            Expression<Func<TokenAllocation, bool>> allocationPredicate = ta =>
                ta.projectTokenConfigId == projectTokenConfigId &&
                ta.holderClass == TokenHolderClass.CONTRIBUTOR;

            var allocations = await _tokenAllocationRepository.FindAsync(allocationPredicate, ct);
            var allocationsList = allocations.ToList();

            foreach (var allocation in allocationsList)
            {
                allocation.isLiquid = true;
                allocation.updatedAt = DateTime.UtcNow;
                await _tokenAllocationRepository.UpdateAsync(allocation, ct);
            }

            // 5. Unlock TokenBalances for CONTRIBUTOR holder class
            Expression<Func<TokenBalance, bool>> balancePredicate = tb =>
                tb.projectTokenConfigId == projectTokenConfigId &&
                tb.holderClass == TokenHolderClass.CONTRIBUTOR;

            var balances = await _tokenBalanceRepository.FindAsync(balancePredicate, ct);
            var balancesList = balances.ToList();

            foreach (var balance in balancesList)
            {
                balance.isLiquid = true;
                balance.updatedAt = DateTime.UtcNow;
                await _tokenBalanceRepository.UpdateAsync(balance, ct);
            }

            // 6. Count total tokens unlocked
            var tokensUnlocked = allocationsList.Sum(a => a.tokenAmount) +
                                balancesList.Sum(b => b.balance);

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

            // 3. Unlock TokenAllocations for INVESTOR and FOUNDER holder classes
            Expression<Func<TokenAllocation, bool>> allocationPredicate = ta =>
                ta.projectTokenConfigId == projectTokenConfigId &&
                (ta.holderClass == TokenHolderClass.INVESTOR || ta.holderClass == TokenHolderClass.FOUNDER);

            var allocations = await _tokenAllocationRepository.FindAsync(allocationPredicate, ct);
            var allocationsList = allocations.ToList();

            foreach (var allocation in allocationsList)
            {
                allocation.isLiquid = true;
                allocation.updatedAt = DateTime.UtcNow;
                await _tokenAllocationRepository.UpdateAsync(allocation, ct);
            }

            // 4. Unlock TokenBalances for INVESTOR and FOUNDER holder classes
            Expression<Func<TokenBalance, bool>> balancePredicate = tb =>
                tb.projectTokenConfigId == projectTokenConfigId &&
                (tb.holderClass == TokenHolderClass.INVESTOR || tb.holderClass == TokenHolderClass.FOUNDER);

            var balances = await _tokenBalanceRepository.FindAsync(balancePredicate, ct);
            var balancesList = balances.ToList();

            foreach (var balance in balancesList)
            {
                balance.isLiquid = true;
                balance.updatedAt = DateTime.UtcNow;
                await _tokenBalanceRepository.UpdateAsync(balance, ct);
            }

            // 5. Count total tokens unlocked
            var tokensUnlocked = allocationsList.Sum(a => a.tokenAmount) +
                                balancesList.Sum(b => b.balance);

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
