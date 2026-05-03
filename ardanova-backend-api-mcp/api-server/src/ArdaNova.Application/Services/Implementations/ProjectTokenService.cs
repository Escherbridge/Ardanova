namespace ArdaNova.Application.Services.Implementations;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using Microsoft.Extensions.Logging;

/// <summary>
/// Manages project token configurations, allocations (by holder class: CONTRIBUTOR, INVESTOR, FOUNDER),
/// distribution, revocation, founder token burning, and investor trust protection.
/// </summary>
public class ProjectTokenService : IProjectTokenService
{
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
        ILogger<ProjectTokenService> logger)
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

        var reservedSupply = (int)(dto.ReservedPercentage * dto.TotalSupply / 100);

        var entity = new ProjectTokenConfig
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            assetName = dto.AssetName,
            unitName = dto.UnitName,
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

    public async Task<Result<TokenAllocationDto>> AllocateToTaskAsync(
        string projectTokenConfigId,
        CreateTokenAllocationDto dto,
        CancellationToken ct = default)
    {
        var config = await _configRepo.GetByIdAsync(projectTokenConfigId, ct);
        if (config == null)
            return Result<TokenAllocationDto>.NotFound(
                $"Project token config {projectTokenConfigId} not found");

        // Validate gateStatus >= ACTIVE
        if (config.gateStatus < ProjectGateStatus.ACTIVE)
            return Result<TokenAllocationDto>.ValidationError(
                $"Cannot allocate to task until project gate status is ACTIVE. Current: {config.gateStatus}");

        // Calculate token amount
        var tokenAmount = (int)(config.totalSupply * dto.EquityPercentage / 100);

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
        CancellationToken ct = default)
    {
        var config = await _configRepo.GetByIdAsync(projectTokenConfigId, ct);
        if (config == null)
            return Result<TokenAllocationDto>.NotFound(
                $"Project token config {projectTokenConfigId} not found");

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
        var isLiquid = DetermineIsLiquid(allocation.holderClass, config.gateStatus);

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

    // Helper methods

    private static bool DetermineIsLiquid(TokenHolderClass holderClass, ProjectGateStatus gateStatus)
    {
        return holderClass switch
        {
            TokenHolderClass.CONTRIBUTOR => gateStatus == ProjectGateStatus.SUCCEEDED,
            TokenHolderClass.INVESTOR => gateStatus >= ProjectGateStatus.ACTIVE,
            TokenHolderClass.FOUNDER => gateStatus == ProjectGateStatus.SUCCEEDED,
            _ => false
        };
    }
}
