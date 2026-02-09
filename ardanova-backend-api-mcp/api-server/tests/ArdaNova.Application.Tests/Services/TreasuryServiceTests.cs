namespace ArdaNova.Application.Tests.Services;

using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;
using Moq;

public class TreasuryServiceTests
{
    private readonly Mock<IRepository<PlatformTreasury>> _treasuryRepo;
    private readonly Mock<IRepository<PlatformTreasuryTransaction>> _transactionRepo;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IMapper> _mapper;
    private readonly TreasuryService _sut;

    public TreasuryServiceTests()
    {
        _treasuryRepo = new Mock<IRepository<PlatformTreasury>>();
        _transactionRepo = new Mock<IRepository<PlatformTreasuryTransaction>>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _mapper = new Mock<IMapper>();
        _sut = new TreasuryService(
            _treasuryRepo.Object,
            _transactionRepo.Object,
            _unitOfWork.Object,
            _mapper.Object
        );
    }

    [Fact]
    public async Task ProcessFundingInflowAsync_ValidAmount_SplitsAndRecords()
    {
        // Arrange
        var projectId = "proj1";
        var inflowAmount = 1000.0;

        var treasury = new PlatformTreasury
        {
            id = "treasury1",
            indexFundBalance = 0.0,
            liquidReserveBalance = 0.0,
            operationsBalance = 0.0,
            indexFundAllocationPct = 0.60,
            liquidReserveAllocationPct = 0.30,
            operationsAllocationPct = 0.10,
            indexFundAnnualReturn = 0.12,
            platformProfitSharePct = 0.20,
            trustProtectionRate = 0.30,
            totalInflows = 0.0,
            totalPayouts = 0.0,
            totalRebalanceTransfers = 0.0,
            ardaTotalSupply = 10000,
            ardaCirculatingSupply = 5000,
            lastReconciliationAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _treasuryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformTreasury> { treasury });

        _treasuryRepo.Setup(r => r.UpdateAsync(It.IsAny<PlatformTreasury>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _transactionRepo.Setup(r => r.AddAsync(It.IsAny<PlatformTreasuryTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformTreasuryTransaction t, CancellationToken ct) => t);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.ProcessFundingInflowAsync(inflowAmount, projectId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        // 60% to index fund = 600
        // 30% to liquid reserve = 300
        // 10% to operations = 100
        treasury.indexFundBalance.Should().Be(600.0);
        treasury.liquidReserveBalance.Should().Be(300.0);
        treasury.operationsBalance.Should().Be(100.0);
        treasury.totalInflows.Should().Be(1000.0);

        _treasuryRepo.Verify(r => r.UpdateAsync(treasury, It.IsAny<CancellationToken>()), Times.Once);
        _transactionRepo.Verify(r => r.AddAsync(
            It.Is<PlatformTreasuryTransaction>(t =>
                t.type == PlatformTreasuryTransactionType.ALLOCATION_INDEX &&
                t.amount == 600.0),
            It.IsAny<CancellationToken>()), Times.Once);
        _transactionRepo.Verify(r => r.AddAsync(
            It.Is<PlatformTreasuryTransaction>(t =>
                t.type == PlatformTreasuryTransactionType.ALLOCATION_LIQUID &&
                t.amount == 300.0),
            It.IsAny<CancellationToken>()), Times.Once);
        _transactionRepo.Verify(r => r.AddAsync(
            It.Is<PlatformTreasuryTransaction>(t =>
                t.type == PlatformTreasuryTransactionType.ALLOCATION_OPS &&
                t.amount == 100.0),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessFundingInflowAsync_NegativeAmount_ReturnsFailure()
    {
        // Arrange
        var projectId = "proj1";
        var inflowAmount = -1000.0;

        // Act
        var result = await _sut.ProcessFundingInflowAsync(inflowAmount, projectId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("must be positive");
    }

    [Fact]
    public async Task ApplyIndexFundReturnAsync_ValidBalance_AppliesReturn()
    {
        // Arrange
        var treasury = new PlatformTreasury
        {
            id = "treasury1",
            indexFundBalance = 12000.0,
            liquidReserveBalance = 3000.0,
            operationsBalance = 1000.0,
            indexFundAllocationPct = 0.60,
            liquidReserveAllocationPct = 0.30,
            operationsAllocationPct = 0.10,
            indexFundAnnualReturn = 0.12,
            platformProfitSharePct = 0.20,
            trustProtectionRate = 0.30,
            totalInflows = 10000.0,
            totalPayouts = 0.0,
            totalRebalanceTransfers = 0.0,
            ardaTotalSupply = 10000,
            ardaCirculatingSupply = 5000,
            lastReconciliationAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _treasuryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformTreasury> { treasury });

        _treasuryRepo.Setup(r => r.UpdateAsync(It.IsAny<PlatformTreasury>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _transactionRepo.Setup(r => r.AddAsync(It.IsAny<PlatformTreasuryTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformTreasuryTransaction t, CancellationToken ct) => t);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.ApplyIndexFundReturnAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // monthlyReturn = 12000 * (0.12 / 12) = 12000 * 0.01 = 120
        // profitShare = 120 * 0.20 = 24
        // indexFundBalance = 12000 + (120 - 24) = 12096
        // operationsBalance = 1000 + 24 = 1024
        treasury.indexFundBalance.Should().Be(12096.0);
        treasury.operationsBalance.Should().Be(1024.0);

        _treasuryRepo.Verify(r => r.UpdateAsync(treasury, It.IsAny<CancellationToken>()), Times.Once);
        _transactionRepo.Verify(r => r.AddAsync(
            It.Is<PlatformTreasuryTransaction>(t =>
                t.type == PlatformTreasuryTransactionType.INDEX_RETURN),
            It.IsAny<CancellationToken>()), Times.Once);
        _transactionRepo.Verify(r => r.AddAsync(
            It.Is<PlatformTreasuryTransaction>(t =>
                t.type == PlatformTreasuryTransactionType.PROFIT_SHARE),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyIndexFundReturnAsync_ZeroBalance_ReturnsFailure()
    {
        // Arrange
        var treasury = new PlatformTreasury
        {
            id = "treasury1",
            indexFundBalance = 0.0,
            liquidReserveBalance = 3000.0,
            operationsBalance = 1000.0,
            indexFundAllocationPct = 0.60,
            liquidReserveAllocationPct = 0.30,
            operationsAllocationPct = 0.10,
            indexFundAnnualReturn = 0.12,
            platformProfitSharePct = 0.20,
            trustProtectionRate = 0.30,
            totalInflows = 10000.0,
            totalPayouts = 0.0,
            totalRebalanceTransfers = 0.0,
            ardaTotalSupply = 10000,
            ardaCirculatingSupply = 5000,
            lastReconciliationAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _treasuryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformTreasury> { treasury });

        // Act
        var result = await _sut.ApplyIndexFundReturnAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("zero");
    }

    [Fact]
    public async Task RebalanceIfNeededAsync_SufficientLiquid_ReturnsZero()
    {
        // Arrange
        var requiredLiquid = 2000.0;

        var treasury = new PlatformTreasury
        {
            id = "treasury1",
            indexFundBalance = 6000.0,
            liquidReserveBalance = 3000.0,
            operationsBalance = 1000.0,
            indexFundAllocationPct = 0.60,
            liquidReserveAllocationPct = 0.30,
            operationsAllocationPct = 0.10,
            indexFundAnnualReturn = 0.12,
            platformProfitSharePct = 0.20,
            trustProtectionRate = 0.30,
            totalInflows = 10000.0,
            totalPayouts = 0.0,
            totalRebalanceTransfers = 0.0,
            ardaTotalSupply = 10000,
            ardaCirculatingSupply = 5000,
            lastReconciliationAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _treasuryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformTreasury> { treasury });

        // Act
        var result = await _sut.RebalanceIfNeededAsync(requiredLiquid, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0.0);
        // liquidReserveBalance = 3000 >= requiredLiquid = 2000
        // No transfer needed
        treasury.liquidReserveBalance.Should().Be(3000.0);
        treasury.indexFundBalance.Should().Be(6000.0);
    }

    [Fact]
    public async Task RebalanceIfNeededAsync_Deficit_TransfersFromIndex()
    {
        // Arrange
        var requiredLiquid = 5000.0;

        var treasury = new PlatformTreasury
        {
            id = "treasury1",
            indexFundBalance = 6000.0,
            liquidReserveBalance = 3000.0,
            operationsBalance = 1000.0,
            indexFundAllocationPct = 0.60,
            liquidReserveAllocationPct = 0.30,
            operationsAllocationPct = 0.10,
            indexFundAnnualReturn = 0.12,
            platformProfitSharePct = 0.20,
            trustProtectionRate = 0.30,
            totalInflows = 10000.0,
            totalPayouts = 0.0,
            totalRebalanceTransfers = 0.0,
            ardaTotalSupply = 10000,
            ardaCirculatingSupply = 5000,
            lastReconciliationAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _treasuryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformTreasury> { treasury });

        _treasuryRepo.Setup(r => r.UpdateAsync(It.IsAny<PlatformTreasury>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _transactionRepo.Setup(r => r.AddAsync(It.IsAny<PlatformTreasuryTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformTreasuryTransaction t, CancellationToken ct) => t);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.RebalanceIfNeededAsync(requiredLiquid, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // deficit = 5000 - 3000 = 2000
        // transfer = min(2000, 6000) = 2000
        result.Value.Should().Be(2000.0);
        treasury.liquidReserveBalance.Should().Be(5000.0);
        treasury.indexFundBalance.Should().Be(4000.0);
        treasury.totalRebalanceTransfers.Should().Be(2000.0);

        _treasuryRepo.Verify(r => r.UpdateAsync(treasury, It.IsAny<CancellationToken>()), Times.Once);
        _transactionRepo.Verify(r => r.AddAsync(
            It.Is<PlatformTreasuryTransaction>(t =>
                t.type == PlatformTreasuryTransactionType.REBALANCE &&
                t.amount == 2000.0),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RebalanceIfNeededAsync_InsufficientIndexFund_ReturnsFailure()
    {
        // Arrange
        var requiredLiquid = 5000.0;

        var treasury = new PlatformTreasury
        {
            id = "treasury1",
            indexFundBalance = 0.0,
            liquidReserveBalance = 3000.0,
            operationsBalance = 1000.0,
            indexFundAllocationPct = 0.60,
            liquidReserveAllocationPct = 0.30,
            operationsAllocationPct = 0.10,
            indexFundAnnualReturn = 0.12,
            platformProfitSharePct = 0.20,
            trustProtectionRate = 0.30,
            totalInflows = 10000.0,
            totalPayouts = 0.0,
            totalRebalanceTransfers = 0.0,
            ardaTotalSupply = 10000,
            ardaCirculatingSupply = 5000,
            lastReconciliationAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _treasuryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformTreasury> { treasury });

        // Act
        var result = await _sut.RebalanceIfNeededAsync(requiredLiquid, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Insufficient");
    }

    [Fact]
    public async Task ReconcileAsync_PositiveBalance_ReturnsTrue()
    {
        // Arrange
        var treasury = new PlatformTreasury
        {
            id = "treasury1",
            indexFundBalance = 6000.0,
            liquidReserveBalance = 3000.0,
            operationsBalance = 1000.0,
            indexFundAllocationPct = 0.60,
            liquidReserveAllocationPct = 0.30,
            operationsAllocationPct = 0.10,
            indexFundAnnualReturn = 0.12,
            platformProfitSharePct = 0.20,
            trustProtectionRate = 0.30,
            totalInflows = 10000.0,
            totalPayouts = 0.0,
            totalRebalanceTransfers = 0.0,
            ardaTotalSupply = 10000,
            ardaCirculatingSupply = 5000,
            lastReconciliationAt = DateTime.UtcNow.AddDays(-1),
            updatedAt = DateTime.UtcNow
        };

        _treasuryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformTreasury> { treasury });

        _treasuryRepo.Setup(r => r.UpdateAsync(It.IsAny<PlatformTreasury>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.ReconcileAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        treasury.lastReconciliationAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _treasuryRepo.Verify(r => r.UpdateAsync(treasury, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReconcileAsync_ZeroBalance_ReturnsFailure()
    {
        // Arrange
        var treasury = new PlatformTreasury
        {
            id = "treasury1",
            indexFundBalance = 0.0,
            liquidReserveBalance = 0.0,
            operationsBalance = 0.0,
            indexFundAllocationPct = 0.60,
            liquidReserveAllocationPct = 0.30,
            operationsAllocationPct = 0.10,
            indexFundAnnualReturn = 0.12,
            platformProfitSharePct = 0.20,
            trustProtectionRate = 0.30,
            totalInflows = 0.0,
            totalPayouts = 0.0,
            totalRebalanceTransfers = 0.0,
            ardaTotalSupply = 10000,
            ardaCirculatingSupply = 5000,
            lastReconciliationAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _treasuryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformTreasury> { treasury });

        // Act
        var result = await _sut.ReconcileAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("zero");
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsMappedDto()
    {
        // Arrange
        var treasury = new PlatformTreasury
        {
            id = "treasury1",
            indexFundBalance = 6000.0,
            liquidReserveBalance = 3000.0,
            operationsBalance = 1000.0,
            indexFundAllocationPct = 0.60,
            liquidReserveAllocationPct = 0.30,
            operationsAllocationPct = 0.10,
            indexFundAnnualReturn = 0.12,
            platformProfitSharePct = 0.20,
            trustProtectionRate = 0.30,
            totalInflows = 10000.0,
            totalPayouts = 0.0,
            totalRebalanceTransfers = 0.0,
            ardaTotalSupply = 10000,
            ardaCirculatingSupply = 5000,
            lastReconciliationAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        var treasuryDto = new TreasuryStatusDto
        {
            Id = treasury.id,
            IndexFundBalance = treasury.indexFundBalance,
            LiquidReserveBalance = treasury.liquidReserveBalance,
            OperationsBalance = treasury.operationsBalance
        };

        _treasuryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformTreasury> { treasury });

        _mapper.Setup(m => m.Map<TreasuryStatusDto>(treasury))
            .Returns(treasuryDto);

        // Act
        var result = await _sut.GetStatusAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(treasuryDto);
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_ReturnsOrderedLimited()
    {
        // Arrange
        var limit = 10;
        var offset = 0;

        var transactions = new List<PlatformTreasuryTransaction>
        {
            new PlatformTreasuryTransaction
            {
                id = "txn1",
                type = PlatformTreasuryTransactionType.FUNDING_INFLOW,
                amount = 1000.0,
                balanceAfter = 1000.0,
                description = "Funding inflow",
                relatedProjectId = "proj1",
                fromBucket = null,
                toBucket = null,
                createdAt = DateTime.UtcNow.AddDays(-2)
            },
            new PlatformTreasuryTransaction
            {
                id = "txn2",
                type = PlatformTreasuryTransactionType.ALLOCATION_INDEX,
                amount = 600.0,
                balanceAfter = 600.0,
                description = "Allocation to index fund",
                relatedProjectId = "proj1",
                fromBucket = null,
                toBucket = "IndexFund",
                createdAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        var transactionDtos = transactions.Select(t => new PlatformTreasuryTransactionDto
        {
            Id = t.id,
            Type = t.type,
            Amount = t.amount,
            Description = t.description
        }).ToList();

        _transactionRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        _mapper.Setup(m => m.Map<List<PlatformTreasuryTransactionDto>>(It.IsAny<IEnumerable<PlatformTreasuryTransaction>>()))
            .Returns(transactionDtos);

        // Act
        var result = await _sut.GetTransactionHistoryAsync(limit, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().BeEquivalentTo(transactionDtos);
    }
}
