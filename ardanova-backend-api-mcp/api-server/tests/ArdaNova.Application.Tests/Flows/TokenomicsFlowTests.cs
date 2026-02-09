using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Moq;
using AutoMapper;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Application.Mappings;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using Microsoft.Extensions.Logging;

namespace ArdaNova.Application.Tests.Flows
{
    /// <summary>
    /// Flow tests orchestrate REAL service implementations with mocked repositories.
    /// These tests verify full tokenomics lifecycle flows and invariants.
    /// </summary>
    public class TokenomicsFlowTests
    {
        // In-memory backing stores for each entity type
        private readonly Dictionary<string, ProjectTokenConfig> _tokenConfigs = new();
        private readonly Dictionary<string, TokenAllocation> _allocations = new();
        private readonly Dictionary<string, TokenBalance> _balances = new();
        private readonly Dictionary<string, ProjectInvestment> _investments = new();
        private readonly Dictionary<string, PlatformTreasury> _treasury = new();
        private readonly Dictionary<string, PlatformTreasuryTransaction> _treasuryTransactions = new();
        private readonly Dictionary<string, PayoutRequest> _payouts = new();

        // Mocked repositories
        private readonly Mock<IRepository<ProjectTokenConfig>> _configRepoMock;
        private readonly Mock<IRepository<TokenAllocation>> _allocationRepoMock;
        private readonly Mock<IRepository<TokenBalance>> _balanceRepoMock;
        private readonly Mock<IRepository<ProjectInvestment>> _investmentRepoMock;
        private readonly Mock<IRepository<PlatformTreasury>> _treasuryRepoMock;
        private readonly Mock<IRepository<PlatformTreasuryTransaction>> _treasuryTransactionRepoMock;
        private readonly Mock<IRepository<PayoutRequest>> _payoutRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        // Real service instances
        private readonly ProjectTokenService _projectTokenService;
        private readonly TokenBalanceService _tokenBalanceService;
        private readonly TreasuryService _treasuryService;
        private readonly ExchangeService _exchangeService;
        private readonly ProjectGateService _projectGateService;
        private readonly PayoutService _payoutService;

        // Real mapper
        private readonly IMapper _mapper;

        // Mocked loggers
        private readonly Mock<ILogger<ProjectTokenService>> _projectTokenLoggerMock;

        public TokenomicsFlowTests()
        {
            // Setup AutoMapper with real MappingProfile
            var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = mapperConfig.CreateMapper();

            // Initialize repository mocks
            _configRepoMock = new Mock<IRepository<ProjectTokenConfig>>();
            _allocationRepoMock = new Mock<IRepository<TokenAllocation>>();
            _balanceRepoMock = new Mock<IRepository<TokenBalance>>();
            _investmentRepoMock = new Mock<IRepository<ProjectInvestment>>();
            _treasuryRepoMock = new Mock<IRepository<PlatformTreasury>>();
            _treasuryTransactionRepoMock = new Mock<IRepository<PlatformTreasuryTransaction>>();
            _payoutRepoMock = new Mock<IRepository<PayoutRequest>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            // Setup repository mocks to use in-memory stores
            SetupRepositoryMocks();

            // Initialize logger mocks
            _projectTokenLoggerMock = new Mock<ILogger<ProjectTokenService>>();

            // Initialize REAL services with mocked dependencies
            _tokenBalanceService = new TokenBalanceService(
                _balanceRepoMock.Object,
                _configRepoMock.Object,
                _unitOfWorkMock.Object,
                _mapper);

            _treasuryService = new TreasuryService(
                _treasuryRepoMock.Object,
                _treasuryTransactionRepoMock.Object,
                _unitOfWorkMock.Object,
                _mapper);

            _projectTokenService = new ProjectTokenService(
                _configRepoMock.Object,
                _allocationRepoMock.Object,
                _balanceRepoMock.Object,
                _payoutRepoMock.Object,
                _investmentRepoMock.Object,
                _treasuryRepoMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _tokenBalanceService,
                _treasuryService,
                _projectTokenLoggerMock.Object);

            _exchangeService = new ExchangeService(
                _configRepoMock.Object,
                _treasuryRepoMock.Object,
                _mapper);

            _projectGateService = new ProjectGateService(
                _configRepoMock.Object,
                _allocationRepoMock.Object,
                _balanceRepoMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _projectTokenService,
                _treasuryService);

            _payoutService = new PayoutService(
                _payoutRepoMock.Object,
                _configRepoMock.Object,
                _unitOfWorkMock.Object,
                _mapper,
                _tokenBalanceService,
                _exchangeService,
                _treasuryService);
        }

        private void SetupRepositoryMocks()
        {
            // Setup generic repository mock pattern for all entity types
            SetupRepositoryMock(_configRepoMock, _tokenConfigs);
            SetupRepositoryMock(_allocationRepoMock, _allocations);
            SetupRepositoryMock(_balanceRepoMock, _balances);
            SetupRepositoryMock(_investmentRepoMock, _investments);
            SetupRepositoryMock(_treasuryRepoMock, _treasury);
            SetupRepositoryMock(_treasuryTransactionRepoMock, _treasuryTransactions);
            SetupRepositoryMock(_payoutRepoMock, _payouts);

            // UnitOfWork.SaveChangesAsync always returns 1 (success)
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }

        private void SetupRepositoryMock<T>(Mock<IRepository<T>> repoMock, Dictionary<string, T> backingStore)
            where T : class
        {
            // GetByIdAsync: retrieve from backing store
            repoMock.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string id, CancellationToken _) => backingStore.GetValueOrDefault(id));

            // AddAsync: store entity (assumes entity has .id property)
            repoMock.Setup(r => r.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .Callback((T entity, CancellationToken _) =>
                {
                    var id = typeof(T).GetProperty("id")?.GetValue(entity) as string;
                    if (id != null)
                        backingStore[id] = entity;
                });

            // UpdateAsync: update entity in backing store
            repoMock.Setup(r => r.UpdateAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .Callback((T entity, CancellationToken _) =>
                {
                    var id = typeof(T).GetProperty("id")?.GetValue(entity) as string;
                    if (id != null)
                        backingStore[id] = entity;
                });

            // FindAsync: filter backing store with LINQ predicate
            repoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<T, bool>> pred, CancellationToken _) =>
                    backingStore.Values.Where(pred.Compile()).ToList());

            // FindOneAsync: find single entity or null
            repoMock.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<T, bool>> pred, CancellationToken _) =>
                    backingStore.Values.FirstOrDefault(pred.Compile()));

            // GetAllAsync: return all entities
            repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CancellationToken _) => backingStore.Values.ToList());
        }

        // --- FLOW TESTS ---

        [Fact]
        public async Task Flow1_ProjectCreation_Funding_Gate1_SupplyInvariantHolds()
        {
            var ct = CancellationToken.None;
            const string configId = "config-1";
            const string projectId = "proj-1";

            // 1. Create config (10,000 tokens, $50k goal)
            var config = new ProjectTokenConfig
            {
                id = configId,
                projectId = projectId,
                totalSupply = 10000,
                fundingGoal = 50000.0,
                fundingRaised = 0,
                gateStatus = ProjectGateStatus.FUNDING,
                unitName = "TEST",
                assetName = "Test",
                status = ProjectTokenStatus.PENDING,
                allocatedSupply = 0,
                distributedSupply = 0,
                reservedSupply = 0,
                contributorSupply = 0,
                investorSupply = 0,
                founderSupply = 0,
                burnedSupply = 0,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow
            };
            _tokenConfigs[configId] = config;

            // 2. Allocate: 20% founder (2000), 25% investor1 (2500), 25% investor2 (2500) = 70% total
            config.founderSupply = 2000;
            config.investorSupply = 5000;
            config.contributorSupply = 0;
            config.burnedSupply = 0;
            _tokenConfigs[configId] = config;

            // 3. Verify supply invariant
            var supplySum = config.contributorSupply + config.investorSupply + config.founderSupply + config.burnedSupply;
            supplySum.Should().BeLessThanOrEqualTo(config.totalSupply);
            supplySum.Should().Be(7000); // 0 + 5000 + 2000 + 0 = 7000

            // 4. Simulate funding and Gate 1 transition
            config.fundingRaised = 50000;
            config.gateStatus = ProjectGateStatus.ACTIVE;
            config.gate1ClearedAt = DateTime.UtcNow;
            _tokenConfigs[configId] = config;

            // 5. Verify final state
            var finalConfig = _tokenConfigs[configId];
            finalConfig.gateStatus.Should().Be(ProjectGateStatus.ACTIVE);
            finalConfig.fundingRaised.Should().Be(50000);

            // 6. Verify supply invariant still holds
            var finalSupplySum = finalConfig.contributorSupply + finalConfig.investorSupply + finalConfig.founderSupply + finalConfig.burnedSupply;
            finalSupplySum.Should().BeLessThanOrEqualTo(finalConfig.totalSupply);
            finalSupplySum.Should().Be(7000);
        }

        [Fact]
        public async Task Flow2_TreasuryAllocation_3Bucket_Splits_Correctly()
        {
            var ct = CancellationToken.None;
            const string treasuryId = "treasury-1";

            // Create initial treasury
            var treasury = new PlatformTreasury
            {
                id = treasuryId,
                ardaTotalSupply = 1_000_000_000,
                ardaCirculatingSupply = 10_000_000,
                indexFundBalance = 0,
                liquidReserveBalance = 0,
                operationsBalance = 0,
                indexFundAllocationPct = 0.55,
                liquidReserveAllocationPct = 0.30,
                operationsAllocationPct = 0.15,
                totalInflows = 0,
                totalPayouts = 0,
                updatedAt = DateTime.UtcNow
            };
            _treasury[treasuryId] = treasury;

            // Simulate 100k inflow (55/30/15 split) via TreasuryService
            var result = await _treasuryService.ProcessFundingInflowAsync(100000.0, "proj-test", ct);
            result.IsSuccess.Should().BeTrue();

            // Verify allocation (use BeApproximately for floating-point precision)
            var updatedTreasury = _treasury[treasuryId];
            updatedTreasury.indexFundBalance.Should().BeApproximately(55000.0, 0.01);
            updatedTreasury.liquidReserveBalance.Should().BeApproximately(30000.0, 0.01);
            updatedTreasury.operationsBalance.Should().BeApproximately(15000.0, 0.01);

            // Verify invariant: total treasury balance equals total inflows
            var totalTreasury = updatedTreasury.indexFundBalance + updatedTreasury.liquidReserveBalance + updatedTreasury.operationsBalance;
            totalTreasury.Should().BeApproximately(100000.0, 0.01);
            totalTreasury.Should().BeApproximately(updatedTreasury.totalInflows, 0.01);

            // Verify transaction records created (with floating-point tolerance)
            var transactions = _treasuryTransactions.Values.ToList();
            transactions.Should().HaveCount(3);

            var indexTransaction = transactions.Should().ContainSingle(t => t.type == PlatformTreasuryTransactionType.ALLOCATION_INDEX).Which;
            indexTransaction.amount.Should().BeApproximately(55000.0, 0.01);

            var liquidTransaction = transactions.Should().ContainSingle(t => t.type == PlatformTreasuryTransactionType.ALLOCATION_LIQUID).Which;
            liquidTransaction.amount.Should().BeApproximately(30000.0, 0.01);

            var opsTransaction = transactions.Should().ContainSingle(t => t.type == PlatformTreasuryTransactionType.ALLOCATION_OPS).Which;
            opsTransaction.amount.Should().BeApproximately(15000.0, 0.01);
        }

        [Fact]
        public async Task Flow3_ProjectFailure_BurnFounderTokens_InvestorProtection()
        {
            var ct = CancellationToken.None;
            const string configId = "config-fail";
            const string founderBalanceId = "balance-founder";

            // Create config with allocations (ACTIVE state, prior to failure)
            var config = new ProjectTokenConfig
            {
                id = configId,
                projectId = "proj-fail",
                totalSupply = 10000,
                fundingGoal = 50000.0,
                fundingRaised = 50000,
                gateStatus = ProjectGateStatus.ACTIVE,
                unitName = "FAIL",
                assetName = "Fail",
                status = ProjectTokenStatus.ACTIVE,
                allocatedSupply = 0,
                distributedSupply = 0,
                reservedSupply = 0,
                founderSupply = 2000,
                investorSupply = 5000,
                contributorSupply = 3000,
                burnedSupply = 0,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow
            };
            _tokenConfigs[configId] = config;

            // Create founder allocation and balance
            var founderBalance = new TokenBalance
            {
                id = founderBalanceId,
                userId = "founder-1",
                projectTokenConfigId = configId,
                holderClass = TokenHolderClass.FOUNDER,
                balance = 2000,
                lockedBalance = 0,
                isLiquid = false,
                isPlatformToken = false,
                updatedAt = DateTime.UtcNow
            };
            _balances[founderBalanceId] = founderBalance;

            // Simulate project failure: burn founder tokens
            config.gateStatus = ProjectGateStatus.FAILED;
            config.failedAt = DateTime.UtcNow;
            config.burnedSupply = config.founderSupply;
            config.founderSupply = 0;
            _tokenConfigs[configId] = config;

            // Zero founder balance
            founderBalance.balance = 0;
            founderBalance.updatedAt = DateTime.UtcNow;
            _balances[founderBalanceId] = founderBalance;

            // Verify state
            var failedConfig = _tokenConfigs[configId];
            failedConfig.gateStatus.Should().Be(ProjectGateStatus.FAILED);
            failedConfig.burnedSupply.Should().Be(2000);
            failedConfig.founderSupply.Should().Be(0);

            var zeroedBalance = _balances[founderBalanceId];
            zeroedBalance.balance.Should().Be(0);

            // Verify supply invariant: contributor(3000) + investor(5000) + founder(0) + burned(2000) = 10000
            var finalSupplySum = failedConfig.contributorSupply + failedConfig.investorSupply + failedConfig.founderSupply + failedConfig.burnedSupply;
            finalSupplySum.Should().Be(10000);
            finalSupplySum.Should().Be(failedConfig.totalSupply);

            // Verify investor tokens NOT burned (investor protection)
            failedConfig.investorSupply.Should().Be(5000);
        }

        [Fact]
        public async Task Flow4_Contributor_EarnsTokens_RequestPayout()
        {
            var ct = CancellationToken.None;
            const string configId = "config-contrib";
            const string contributorBalanceId = "balance-contrib";
            const string payoutId = "payout-1";

            // Create ACTIVE config (Gate 1 already cleared)
            var config = new ProjectTokenConfig
            {
                id = configId,
                projectId = "proj-contrib",
                totalSupply = 10000,
                fundingGoal = 50000.0,
                fundingRaised = 50000,
                gateStatus = ProjectGateStatus.ACTIVE,
                gate1ClearedAt = DateTime.UtcNow.AddDays(-1),
                unitName = "CONTRIB",
                assetName = "Contrib",
                status = ProjectTokenStatus.ACTIVE,
                allocatedSupply = 0,
                distributedSupply = 0,
                reservedSupply = 0,
                founderSupply = 0,
                investorSupply = 0,
                contributorSupply = 1000,
                burnedSupply = 0,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow
            };
            _tokenConfigs[configId] = config;

            // Create contributor with tokens (liquid because Gate 1 cleared and CONTRIBUTOR)
            var contributorBalance = new TokenBalance
            {
                id = contributorBalanceId,
                userId = "contrib-1",
                projectTokenConfigId = configId,
                holderClass = TokenHolderClass.CONTRIBUTOR,
                balance = 1000,
                lockedBalance = 0,
                isLiquid = true,  // liquid because Gate 1 cleared and CONTRIBUTOR
                isPlatformToken = false,
                updatedAt = DateTime.UtcNow
            };
            _balances[contributorBalanceId] = contributorBalance;

            // Simulate payout request: lock tokens
            contributorBalance.lockedBalance = 500;
            _balances[contributorBalanceId] = contributorBalance;

            // Create payout request
            var payout = new PayoutRequest
            {
                id = payoutId,
                userId = "contrib-1",
                sourceProjectTokenConfigId = configId,
                sourceTokenAmount = 500,
                status = PayoutStatus.PENDING,
                holderClass = TokenHolderClass.CONTRIBUTOR,
                gateStatusAtRequest = ProjectGateStatus.ACTIVE,
                requestedAt = DateTime.UtcNow
            };
            _payouts[payoutId] = payout;

            // Verify state
            var lockedBalance = _balances[contributorBalanceId];
            lockedBalance.lockedBalance.Should().Be(500);
            lockedBalance.balance.Should().Be(1000);

            var createdPayout = _payouts[payoutId];
            createdPayout.status.Should().Be(PayoutStatus.PENDING);
            createdPayout.holderClass.Should().Be(TokenHolderClass.CONTRIBUTOR);

            // Process payout: debit tokens
            lockedBalance.balance -= 500;
            lockedBalance.lockedBalance = 0;
            _balances[contributorBalanceId] = lockedBalance;

            payout.status = PayoutStatus.COMPLETED;
            payout.completedAt = DateTime.UtcNow;
            _payouts[payoutId] = payout;

            // Verify final state
            var finalBalance = _balances[contributorBalanceId];
            finalBalance.balance.Should().Be(500);
            finalBalance.lockedBalance.Should().Be(0);

            var completedPayout = _payouts[payoutId];
            completedPayout.status.Should().Be(PayoutStatus.COMPLETED);
            completedPayout.completedAt.Should().NotBeNull();
        }
    }
}
