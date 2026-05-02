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
using Microsoft.Extensions.Logging;
using Moq;

public class ProjectTokenServiceTests
{
    private readonly Mock<IRepository<ProjectTokenConfig>> _configRepoMock;
    private readonly Mock<IRepository<TokenAllocation>> _allocationRepoMock;
    private readonly Mock<IRepository<TokenBalance>> _balanceRepoMock;
    private readonly Mock<IRepository<PayoutRequest>> _payoutRepoMock;
    private readonly Mock<IRepository<ProjectInvestment>> _investmentRepoMock;
    private readonly Mock<IRepository<PlatformTreasury>> _treasuryRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ITokenBalanceService> _tokenBalanceServiceMock;
    private readonly Mock<ITreasuryService> _treasuryServiceMock;
    private readonly Mock<ILogger<ProjectTokenService>> _loggerMock;
    private readonly ProjectTokenService _sut;

    public ProjectTokenServiceTests()
    {
        _configRepoMock = new Mock<IRepository<ProjectTokenConfig>>();
        _allocationRepoMock = new Mock<IRepository<TokenAllocation>>();
        _balanceRepoMock = new Mock<IRepository<TokenBalance>>();
        _payoutRepoMock = new Mock<IRepository<PayoutRequest>>();
        _investmentRepoMock = new Mock<IRepository<ProjectInvestment>>();
        _treasuryRepoMock = new Mock<IRepository<PlatformTreasury>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _tokenBalanceServiceMock = new Mock<ITokenBalanceService>();
        _treasuryServiceMock = new Mock<ITreasuryService>();
        _loggerMock = new Mock<ILogger<ProjectTokenService>>();

        _sut = new ProjectTokenService(
            _configRepoMock.Object,
            _allocationRepoMock.Object,
            _balanceRepoMock.Object,
            _payoutRepoMock.Object,
            _investmentRepoMock.Object,
            _treasuryRepoMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _tokenBalanceServiceMock.Object,
            _treasuryServiceMock.Object,
            _loggerMock.Object);
    }

    // ========================================================================
    // CreateConfigAsync — Happy Path
    // ========================================================================

    [Fact]
    public async Task CreateConfigAsync_HappyPath_CreatesConfigAndReturnsDto()
    {
        // Arrange
        var dto = new CreateProjectTokenConfigDto
        {
            ProjectId = "project-1",
            AssetName = "Test Token",
            UnitName = "TST",
            TotalSupply = 1000000,
            ReservedPercentage = 20,
            FundingGoal = 50000,
            SuccessCriteria = "Deliver MVP"
        };

        _configRepoMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<ProjectTokenConfig, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTokenConfig?)null);

        _configRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTokenConfig config, CancellationToken ct) => config);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var expectedDto = new ProjectTokenConfigDto
        {
            Id = "config-1",
            ProjectId = "project-1",
            AssetName = "Test Token",
            UnitName = "TST",
            TotalSupply = 1000000,
            ReservedSupply = 200000,
            FounderSupply = 200000
        };

        _mapperMock
            .Setup(m => m.Map<ProjectTokenConfigDto>(It.IsAny<ProjectTokenConfig>()))
            .Returns(expectedDto);

        // Act
        var result = await _sut.CreateConfigAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ProjectId.Should().Be("project-1");
        result.Value!.TotalSupply.Should().Be(1000000);
        result.Value!.ReservedSupply.Should().Be(200000);

        _configRepoMock.Verify(r => r.AddAsync(It.Is<ProjectTokenConfig>(c =>
            c.projectId == "project-1" &&
            c.totalSupply == 1000000 &&
            c.reservedSupply == 200000 &&
            c.founderSupply == 200000 &&
            c.status == ProjectTokenStatus.PENDING &&
            c.gateStatus == ProjectGateStatus.FUNDING
        ), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // CreateConfigAsync — Duplicate ProjectId
    // ========================================================================

    [Fact]
    public async Task CreateConfigAsync_DuplicateProjectId_ReturnsValidationError()
    {
        // Arrange
        var dto = new CreateProjectTokenConfigDto
        {
            ProjectId = "project-1",
            AssetName = "Test Token",
            UnitName = "TST",
            TotalSupply = 1000000,
            ReservedPercentage = 20,
            FundingGoal = 50000,
            SuccessCriteria = "Deliver MVP"
        };

        var existingConfig = new ProjectTokenConfig
        {
            id = "existing-config",
            projectId = "project-1"
        };

        _configRepoMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<ProjectTokenConfig, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConfig);

        // Act
        var result = await _sut.CreateConfigAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("already exists");
        result.Error.Should().Contain("project-1");

        _configRepoMock.Verify(r => r.AddAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ========================================================================
    // GetConfigByIdAsync — Found
    // ========================================================================

    [Fact]
    public async Task GetConfigByIdAsync_Found_ReturnsSuccess()
    {
        // Arrange
        var config = new ProjectTokenConfig
        {
            id = "config-1",
            projectId = "project-1",
            assetName = "Test Token",
            totalSupply = 1000000
        };

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var dto = new ProjectTokenConfigDto
        {
            Id = "config-1",
            ProjectId = "project-1"
        };

        _mapperMock
            .Setup(m => m.Map<ProjectTokenConfigDto>(config))
            .Returns(dto);

        // Act
        var result = await _sut.GetConfigByIdAsync("config-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be("config-1");
    }

    // ========================================================================
    // GetConfigByIdAsync — Not Found
    // ========================================================================

    [Fact]
    public async Task GetConfigByIdAsync_NotFound_ReturnsNotFound()
    {
        // Arrange
        _configRepoMock
            .Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTokenConfig?)null);

        // Act
        var result = await _sut.GetConfigByIdAsync("nonexistent");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
        result.Error.Should().Contain("nonexistent");
    }

    // ========================================================================
    // GetConfigByProjectIdAsync — Found
    // ========================================================================

    [Fact]
    public async Task GetConfigByProjectIdAsync_Found_ReturnsSuccess()
    {
        // Arrange
        var config = new ProjectTokenConfig
        {
            id = "config-1",
            projectId = "project-1",
            assetName = "Test Token"
        };

        _configRepoMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<ProjectTokenConfig, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var dto = new ProjectTokenConfigDto
        {
            Id = "config-1",
            ProjectId = "project-1"
        };

        _mapperMock
            .Setup(m => m.Map<ProjectTokenConfigDto>(config))
            .Returns(dto);

        // Act
        var result = await _sut.GetConfigByProjectIdAsync("project-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ProjectId.Should().Be("project-1");
    }

    // ========================================================================
    // GetConfigByProjectIdAsync — Not Found
    // ========================================================================

    [Fact]
    public async Task GetConfigByProjectIdAsync_NotFound_ReturnsNotFound()
    {
        // Arrange
        _configRepoMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<ProjectTokenConfig, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTokenConfig?)null);

        // Act
        var result = await _sut.GetConfigByProjectIdAsync("nonexistent");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
        result.Error.Should().Contain("nonexistent");
    }

    // ========================================================================
    // AllocateToTaskAsync — Happy Path (ACTIVE Gate)
    // ========================================================================

    [Fact]
    public async Task AllocateToTaskAsync_ActiveGate_CreatesContributorAllocation()
    {
        // Arrange
        var config = new ProjectTokenConfig
        {
            id = "config-1",
            projectId = "project-1",
            totalSupply = 1000000,
            contributorSupply = 100000,
            investorSupply = 200000,
            founderSupply = 200000,
            burnedSupply = 0,
            allocatedSupply = 500000,
            gateStatus = ProjectGateStatus.ACTIVE
        };

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _allocationRepoMock
            .Setup(r => r.AddAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenAllocation alloc, CancellationToken ct) => alloc);

        _configRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new CreateTokenAllocationDto
        {
            TaskId = "task-1",
            EquityPercentage = 5
        };

        var allocationDto = new TokenAllocationDto
        {
            Id = "alloc-1",
            TaskId = "task-1",
            TokenAmount = 50000,
            HolderClass = TokenHolderClass.CONTRIBUTOR
        };

        _mapperMock
            .Setup(m => m.Map<TokenAllocationDto>(It.IsAny<TokenAllocation>()))
            .Returns(allocationDto);

        // Act
        var result = await _sut.AllocateToTaskAsync("config-1", dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.HolderClass.Should().Be(TokenHolderClass.CONTRIBUTOR);
        result.Value!.TokenAmount.Should().Be(50000);

        _allocationRepoMock.Verify(r => r.AddAsync(It.Is<TokenAllocation>(a =>
            a.taskId == "task-1" &&
            a.holderClass == TokenHolderClass.CONTRIBUTOR &&
            a.status == AllocationStatus.RESERVED &&
            a.tokenAmount == 50000 &&
            a.equityPercentage == 5 &&
            a.isLiquid == false
        ), It.IsAny<CancellationToken>()), Times.Once);

        _configRepoMock.Verify(r => r.UpdateAsync(It.Is<ProjectTokenConfig>(c =>
            c.contributorSupply == 150000 &&
            c.allocatedSupply == 550000
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // AllocateToTaskAsync — Gate Not ACTIVE
    // ========================================================================

    [Fact]
    public async Task AllocateToTaskAsync_GateNotActive_ReturnsValidationError()
    {
        // Arrange
        var config = new ProjectTokenConfig
        {
            id = "config-1",
            totalSupply = 1000000,
            gateStatus = ProjectGateStatus.FUNDING
        };

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var dto = new CreateTokenAllocationDto
        {
            TaskId = "task-1",
            EquityPercentage = 5
        };

        // Act
        var result = await _sut.AllocateToTaskAsync("config-1", dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("ACTIVE");
        result.Error.Should().Contain("FUNDING");

        _allocationRepoMock.Verify(r => r.AddAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ========================================================================
    // AllocateToTaskAsync — Insufficient Supply
    // ========================================================================

    [Fact]
    public async Task AllocateToTaskAsync_InsufficientSupply_ReturnsValidationError()
    {
        // Arrange
        var config = new ProjectTokenConfig
        {
            id = "config-1",
            totalSupply = 1000000,
            contributorSupply = 300000,
            investorSupply = 400000,
            founderSupply = 200000,
            burnedSupply = 50000,
            gateStatus = ProjectGateStatus.ACTIVE
        };

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var dto = new CreateTokenAllocationDto
        {
            TaskId = "task-1",
            EquityPercentage = 10 // 100,000 tokens, but only 50,000 available
        };

        // Act
        var result = await _sut.AllocateToTaskAsync("config-1", dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("Insufficient supply");
        result.Error.Should().Contain("100000");
        result.Error.Should().Contain("50000");

        _allocationRepoMock.Verify(r => r.AddAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ========================================================================
    // AllocateToInvestorAsync — Happy Path
    // ========================================================================

    [Fact]
    public async Task AllocateToInvestorAsync_HappyPath_CreatesInvestorAllocationAndInvestment()
    {
        // Arrange
        var config = new ProjectTokenConfig
        {
            id = "config-1",
            projectId = "project-1",
            totalSupply = 1000000,
            contributorSupply = 100000,
            investorSupply = 200000,
            founderSupply = 200000,
            burnedSupply = 0,
            allocatedSupply = 500000,
            fundingRaised = 10000
        };

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _allocationRepoMock
            .Setup(r => r.AddAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenAllocation alloc, CancellationToken ct) => alloc);

        _configRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _investmentRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ProjectInvestment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectInvestment inv, CancellationToken ct) => inv);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new CreateInvestorAllocationDto
        {
            UserId = "user-1",
            TokenAmount = 50000,
            UsdAmount = 5000
        };

        var allocationDto = new TokenAllocationDto
        {
            Id = "alloc-1",
            RecipientUserId = "user-1",
            TokenAmount = 50000,
            HolderClass = TokenHolderClass.INVESTOR
        };

        _mapperMock
            .Setup(m => m.Map<TokenAllocationDto>(It.IsAny<TokenAllocation>()))
            .Returns(allocationDto);

        // Act
        var result = await _sut.AllocateToInvestorAsync("config-1", dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.HolderClass.Should().Be(TokenHolderClass.INVESTOR);

        _allocationRepoMock.Verify(r => r.AddAsync(It.Is<TokenAllocation>(a =>
            a.recipientUserId == "user-1" &&
            a.holderClass == TokenHolderClass.INVESTOR &&
            a.status == AllocationStatus.RESERVED &&
            a.tokenAmount == 50000 &&
            a.equityPercentage == 5.0
        ), It.IsAny<CancellationToken>()), Times.Once);

        _configRepoMock.Verify(r => r.UpdateAsync(It.Is<ProjectTokenConfig>(c =>
            c.fundingRaised == 15000 &&
            c.investorSupply == 250000 &&
            c.allocatedSupply == 550000
        ), It.IsAny<CancellationToken>()), Times.Once);

        _investmentRepoMock.Verify(r => r.AddAsync(It.Is<ProjectInvestment>(i =>
            i.projectTokenConfigId == "config-1" &&
            i.userId == "user-1" &&
            i.usdAmount == 5000 &&
            i.tokenAmount == 50000 &&
            i.protectionEligible == true &&
            i.protectionPaidOut == false
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // AllocateToFounderAsync — Happy Path
    // ========================================================================

    [Fact]
    public async Task AllocateToFounderAsync_HappyPath_CreatesFounderAllocation()
    {
        // Arrange
        var config = new ProjectTokenConfig
        {
            id = "config-1",
            totalSupply = 1000000,
            contributorSupply = 100000,
            investorSupply = 200000,
            founderSupply = 200000,
            allocatedSupply = 500000
        };

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _allocationRepoMock
            .Setup(r => r.AddAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenAllocation alloc, CancellationToken ct) => alloc);

        _configRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new CreateFounderAllocationDto
        {
            UserId = "founder-1",
            EquityPercentage = 10
        };

        var allocationDto = new TokenAllocationDto
        {
            Id = "alloc-1",
            RecipientUserId = "founder-1",
            TokenAmount = 100000,
            HolderClass = TokenHolderClass.FOUNDER
        };

        _mapperMock
            .Setup(m => m.Map<TokenAllocationDto>(It.IsAny<TokenAllocation>()))
            .Returns(allocationDto);

        // Act
        var result = await _sut.AllocateToFounderAsync("config-1", dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.HolderClass.Should().Be(TokenHolderClass.FOUNDER);

        _allocationRepoMock.Verify(r => r.AddAsync(It.Is<TokenAllocation>(a =>
            a.recipientUserId == "founder-1" &&
            a.holderClass == TokenHolderClass.FOUNDER &&
            a.status == AllocationStatus.RESERVED &&
            a.tokenAmount == 100000
        ), It.IsAny<CancellationToken>()), Times.Once);

        _configRepoMock.Verify(r => r.UpdateAsync(It.Is<ProjectTokenConfig>(c =>
            c.allocatedSupply == 600000 &&
            c.founderSupply == 200000 // Not decremented
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // AllocateToFounderAsync — Exceeds Founder Supply
    // ========================================================================

    [Fact]
    public async Task AllocateToFounderAsync_ExceedsFounderSupply_ReturnsValidationError()
    {
        // Arrange
        var config = new ProjectTokenConfig
        {
            id = "config-1",
            totalSupply = 1000000,
            founderSupply = 100000
        };

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var dto = new CreateFounderAllocationDto
        {
            UserId = "founder-1",
            EquityPercentage = 15 // 150,000 tokens
        };

        // Act
        var result = await _sut.AllocateToFounderAsync("config-1", dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("Insufficient founder");
        result.Error.Should().Contain("150000");
        result.Error.Should().Contain("100000");

        _allocationRepoMock.Verify(r => r.AddAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ========================================================================
    // DistributeAsync — Happy Path
    // ========================================================================

    [Fact]
    public async Task DistributeAsync_HappyPath_DistributesAndCreditsBalance()
    {
        // Arrange
        var allocation = new TokenAllocation
        {
            id = "alloc-1",
            projectTokenConfigId = "config-1",
            tokenAmount = 50000,
            status = AllocationStatus.RESERVED,
            holderClass = TokenHolderClass.CONTRIBUTOR
        };

        var config = new ProjectTokenConfig
        {
            id = "config-1",
            gateStatus = ProjectGateStatus.ACTIVE,
            distributedSupply = 100000
        };

        _allocationRepoMock
            .Setup(r => r.GetByIdAsync("alloc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _allocationRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _configRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tokenBalanceServiceMock
            .Setup(s => s.CreditAsync("user-1", "config-1", 50000, TokenHolderClass.CONTRIBUTOR, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto()));

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var allocationDto = new TokenAllocationDto
        {
            Id = "alloc-1",
            Status = AllocationStatus.DISTRIBUTED
        };

        _mapperMock
            .Setup(m => m.Map<TokenAllocationDto>(It.IsAny<TokenAllocation>()))
            .Returns(allocationDto);

        // Act
        var result = await _sut.DistributeAsync("alloc-1", "user-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(AllocationStatus.DISTRIBUTED);

        _allocationRepoMock.Verify(r => r.UpdateAsync(It.Is<TokenAllocation>(a =>
            a.recipientUserId == "user-1" &&
            a.status == AllocationStatus.DISTRIBUTED &&
            a.isLiquid == false && // CONTRIBUTOR not liquid at ACTIVE
            a.distributedAt != null
        ), It.IsAny<CancellationToken>()), Times.Once);

        _configRepoMock.Verify(r => r.UpdateAsync(It.Is<ProjectTokenConfig>(c =>
            c.distributedSupply == 150000
        ), It.IsAny<CancellationToken>()), Times.Once);

        _tokenBalanceServiceMock.Verify(s => s.CreditAsync(
            "user-1",
            "config-1",
            50000,
            TokenHolderClass.CONTRIBUTOR,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // DistributeAsync — Not RESERVED
    // ========================================================================

    [Fact]
    public async Task DistributeAsync_NotReserved_ReturnsValidationError()
    {
        // Arrange
        var allocation = new TokenAllocation
        {
            id = "alloc-1",
            status = AllocationStatus.DISTRIBUTED
        };

        _allocationRepoMock
            .Setup(r => r.GetByIdAsync("alloc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);

        // Act
        var result = await _sut.DistributeAsync("alloc-1", "user-1");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("RESERVED");
        result.Error.Should().Contain("DISTRIBUTED");

        _tokenBalanceServiceMock.Verify(s => s.CreditAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<TokenHolderClass>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ========================================================================
    // DistributeAsync — CreditAsync Fails
    // ========================================================================

    [Fact]
    public async Task DistributeAsync_CreditAsyncFails_ReturnsFailure()
    {
        // Arrange
        var allocation = new TokenAllocation
        {
            id = "alloc-1",
            projectTokenConfigId = "config-1",
            tokenAmount = 50000,
            status = AllocationStatus.RESERVED,
            holderClass = TokenHolderClass.CONTRIBUTOR
        };

        var config = new ProjectTokenConfig
        {
            id = "config-1",
            gateStatus = ProjectGateStatus.ACTIVE
        };

        _allocationRepoMock
            .Setup(r => r.GetByIdAsync("alloc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _allocationRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _configRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tokenBalanceServiceMock
            .Setup(s => s.CreditAsync("user-1", "config-1", 50000, TokenHolderClass.CONTRIBUTOR, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Failure("Balance service unavailable"));

        // Act
        var result = await _sut.DistributeAsync("alloc-1", "user-1");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("failed to credit balance");
        result.Error.Should().Contain("Balance service unavailable");
    }

    // ========================================================================
    // RevokeAllocationAsync — Happy Path (CONTRIBUTOR)
    // ========================================================================

    [Fact]
    public async Task RevokeAllocationAsync_ContributorAllocation_DecrementsContributorSupply()
    {
        // Arrange
        var allocation = new TokenAllocation
        {
            id = "alloc-1",
            projectTokenConfigId = "config-1",
            tokenAmount = 50000,
            status = AllocationStatus.RESERVED,
            holderClass = TokenHolderClass.CONTRIBUTOR
        };

        var config = new ProjectTokenConfig
        {
            id = "config-1",
            contributorSupply = 150000,
            allocatedSupply = 500000
        };

        _allocationRepoMock
            .Setup(r => r.GetByIdAsync("alloc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _allocationRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _configRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var allocationDto = new TokenAllocationDto
        {
            Id = "alloc-1",
            Status = AllocationStatus.REVOKED
        };

        _mapperMock
            .Setup(m => m.Map<TokenAllocationDto>(It.IsAny<TokenAllocation>()))
            .Returns(allocationDto);

        // Act
        var result = await _sut.RevokeAllocationAsync("alloc-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(AllocationStatus.REVOKED);

        _allocationRepoMock.Verify(r => r.UpdateAsync(It.Is<TokenAllocation>(a =>
            a.status == AllocationStatus.REVOKED
        ), It.IsAny<CancellationToken>()), Times.Once);

        _configRepoMock.Verify(r => r.UpdateAsync(It.Is<ProjectTokenConfig>(c =>
            c.contributorSupply == 100000 &&
            c.allocatedSupply == 450000
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // RevokeAllocationAsync — Already REVOKED
    // ========================================================================

    [Fact]
    public async Task RevokeAllocationAsync_AlreadyRevoked_ReturnsValidationError()
    {
        // Arrange
        var allocation = new TokenAllocation
        {
            id = "alloc-1",
            status = AllocationStatus.REVOKED
        };

        _allocationRepoMock
            .Setup(r => r.GetByIdAsync("alloc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);

        // Act
        var result = await _sut.RevokeAllocationAsync("alloc-1");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("already revoked");

        _configRepoMock.Verify(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ========================================================================
    // RevokeAllocationAsync — BURNED
    // ========================================================================

    [Fact]
    public async Task RevokeAllocationAsync_Burned_ReturnsValidationError()
    {
        // Arrange
        var allocation = new TokenAllocation
        {
            id = "alloc-1",
            status = AllocationStatus.BURNED
        };

        _allocationRepoMock
            .Setup(r => r.GetByIdAsync("alloc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);

        // Act
        var result = await _sut.RevokeAllocationAsync("alloc-1");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("Cannot revoke burned allocation");

        _configRepoMock.Verify(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ========================================================================
    // BurnFounderTokensAsync — Happy Path
    // ========================================================================

    [Fact]
    public async Task BurnFounderTokensAsync_HappyPath_BurnsAllFounderAllocations()
    {
        // Arrange
        var config = new ProjectTokenConfig
        {
            id = "config-1",
            founderSupply = 200000,
            burnedSupply = 0
        };

        var founderAllocations = new List<TokenAllocation>
        {
            new TokenAllocation
            {
                id = "alloc-1",
                projectTokenConfigId = "config-1",
                recipientUserId = "founder-1",
                tokenAmount = 100000,
                status = AllocationStatus.DISTRIBUTED,
                holderClass = TokenHolderClass.FOUNDER
            },
            new TokenAllocation
            {
                id = "alloc-2",
                projectTokenConfigId = "config-1",
                recipientUserId = "founder-2",
                tokenAmount = 50000,
                status = AllocationStatus.RESERVED,
                holderClass = TokenHolderClass.FOUNDER
            }
        };

        var balances = new List<TokenBalance>
        {
            new TokenBalance
            {
                id = "balance-1",
                userId = "founder-1",
                projectTokenConfigId = "config-1",
                holderClass = TokenHolderClass.FOUNDER,
                balance = 100000,
                lockedBalance = 0
            }
        };

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _allocationRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<TokenAllocation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(founderAllocations);

        _allocationRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _balanceRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<TokenBalance, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(balances);

        _balanceRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<TokenBalance>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _configRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.BurnFounderTokensAsync("config-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        _allocationRepoMock.Verify(r => r.UpdateAsync(It.Is<TokenAllocation>(a =>
            a.status == AllocationStatus.BURNED &&
            a.burnedAt != null
        ), It.IsAny<CancellationToken>()), Times.Exactly(2));

        _balanceRepoMock.Verify(r => r.UpdateAsync(It.Is<TokenBalance>(b =>
            b.balance == 0 &&
            b.lockedBalance == 0
        ), It.IsAny<CancellationToken>()), Times.Exactly(2));

        _configRepoMock.Verify(r => r.UpdateAsync(It.Is<ProjectTokenConfig>(c =>
            c.burnedSupply == 150000 &&
            c.founderSupply == 50000
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // ProcessInvestorTrustProtectionAsync — Happy Path
    // ========================================================================

    [Fact]
    public async Task ProcessInvestorTrustProtectionAsync_HappyPath_CreatesPayoutRequests()
    {
        // Arrange
        var config = new ProjectTokenConfig
        {
            id = "config-1",
            gateStatus = ProjectGateStatus.FAILED
        };

        var treasury = new PlatformTreasury
        {
            id = "treasury-1",
            trustProtectionRate = 0.5
        };

        var investments = new List<ProjectInvestment>
        {
            new ProjectInvestment
            {
                id = "inv-1",
                projectTokenConfigId = "config-1",
                userId = "investor-1",
                usdAmount = 10000,
                tokenAmount = 50000,
                protectionEligible = true,
                protectionPaidOut = false
            },
            new ProjectInvestment
            {
                id = "inv-2",
                projectTokenConfigId = "config-1",
                userId = "investor-2",
                usdAmount = 5000,
                tokenAmount = 25000,
                protectionEligible = true,
                protectionPaidOut = false
            }
        };

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _treasuryRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformTreasury> { treasury });

        _investmentRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProjectInvestment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(investments);

        _payoutRepoMock
            .Setup(r => r.AddAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PayoutRequest p, CancellationToken ct) => p);

        _investmentRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<ProjectInvestment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.ProcessInvestorTrustProtectionAsync("config-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        _payoutRepoMock.Verify(r => r.AddAsync(It.Is<PayoutRequest>(p =>
            p.userId == "investor-1" &&
            p.sourceProjectTokenConfigId == "config-1" &&
            p.sourceTokenAmount == 0 &&
            p.usdAmount == 5000 &&
            p.status == PayoutStatus.PENDING &&
            p.holderClass == TokenHolderClass.INVESTOR &&
            p.gateStatusAtRequest == ProjectGateStatus.FAILED
        ), It.IsAny<CancellationToken>()), Times.Once);

        _payoutRepoMock.Verify(r => r.AddAsync(It.Is<PayoutRequest>(p =>
            p.userId == "investor-2" &&
            p.usdAmount == 2500
        ), It.IsAny<CancellationToken>()), Times.Once);

        _investmentRepoMock.Verify(r => r.UpdateAsync(It.Is<ProjectInvestment>(i =>
            i.protectionAmount == 5000 &&
            i.protectionPaidOut == true &&
            i.protectionPaidAt != null
        ), It.IsAny<CancellationToken>()), Times.Once);

        _investmentRepoMock.Verify(r => r.UpdateAsync(It.Is<ProjectInvestment>(i =>
            i.protectionAmount == 2500 &&
            i.protectionPaidOut == true
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // GetAllocationsByProjectAsync — Returns Mapped List
    // ========================================================================

    [Fact]
    public async Task GetAllocationsByProjectAsync_ReturnsAllAllocations()
    {
        // Arrange
        var allocations = new List<TokenAllocation>
        {
            new TokenAllocation { id = "alloc-1", projectTokenConfigId = "config-1" },
            new TokenAllocation { id = "alloc-2", projectTokenConfigId = "config-1" }
        };

        _allocationRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<TokenAllocation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);

        var dtos = new List<TokenAllocationDto>
        {
            new TokenAllocationDto { Id = "alloc-1" },
            new TokenAllocationDto { Id = "alloc-2" }
        };

        _mapperMock
            .Setup(m => m.Map<IReadOnlyList<TokenAllocationDto>>(allocations))
            .Returns(dtos);

        // Act
        var result = await _sut.GetAllocationsByProjectAsync("config-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    // ========================================================================
    // DistributeAsync — INVESTOR Liquid at ACTIVE
    // ========================================================================

    [Fact]
    public async Task DistributeAsync_InvestorAtActive_IsLiquid()
    {
        // Arrange
        var allocation = new TokenAllocation
        {
            id = "alloc-1",
            projectTokenConfigId = "config-1",
            tokenAmount = 50000,
            status = AllocationStatus.RESERVED,
            holderClass = TokenHolderClass.INVESTOR
        };

        var config = new ProjectTokenConfig
        {
            id = "config-1",
            gateStatus = ProjectGateStatus.ACTIVE,
            distributedSupply = 0
        };

        _allocationRepoMock
            .Setup(r => r.GetByIdAsync("alloc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _allocationRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _configRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tokenBalanceServiceMock
            .Setup(s => s.CreditAsync("user-1", "config-1", 50000, TokenHolderClass.INVESTOR, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto()));

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock
            .Setup(m => m.Map<TokenAllocationDto>(It.IsAny<TokenAllocation>()))
            .Returns(new TokenAllocationDto());

        // Act
        var result = await _sut.DistributeAsync("alloc-1", "user-1");

        // Assert
        result.IsSuccess.Should().BeTrue();

        _allocationRepoMock.Verify(r => r.UpdateAsync(It.Is<TokenAllocation>(a =>
            a.isLiquid == true // INVESTOR is liquid at ACTIVE
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // DistributeAsync — CONTRIBUTOR Liquid at SUCCEEDED
    // ========================================================================

    [Fact]
    public async Task DistributeAsync_ContributorAtSucceeded_IsLiquid()
    {
        // Arrange
        var allocation = new TokenAllocation
        {
            id = "alloc-1",
            projectTokenConfigId = "config-1",
            tokenAmount = 50000,
            status = AllocationStatus.RESERVED,
            holderClass = TokenHolderClass.CONTRIBUTOR
        };

        var config = new ProjectTokenConfig
        {
            id = "config-1",
            gateStatus = ProjectGateStatus.SUCCEEDED,
            distributedSupply = 0
        };

        _allocationRepoMock
            .Setup(r => r.GetByIdAsync("alloc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _allocationRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _configRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tokenBalanceServiceMock
            .Setup(s => s.CreditAsync("user-1", "config-1", 50000, TokenHolderClass.CONTRIBUTOR, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto()));

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock
            .Setup(m => m.Map<TokenAllocationDto>(It.IsAny<TokenAllocation>()))
            .Returns(new TokenAllocationDto());

        // Act
        var result = await _sut.DistributeAsync("alloc-1", "user-1");

        // Assert
        result.IsSuccess.Should().BeTrue();

        _allocationRepoMock.Verify(r => r.UpdateAsync(It.Is<TokenAllocation>(a =>
            a.isLiquid == true // CONTRIBUTOR is liquid at SUCCEEDED
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // DistributeAsync — FOUNDER Liquid at SUCCEEDED
    // ========================================================================

    [Fact]
    public async Task DistributeAsync_FounderAtSucceeded_IsLiquid()
    {
        // Arrange
        var allocation = new TokenAllocation
        {
            id = "alloc-1",
            projectTokenConfigId = "config-1",
            tokenAmount = 100000,
            status = AllocationStatus.RESERVED,
            holderClass = TokenHolderClass.FOUNDER
        };

        var config = new ProjectTokenConfig
        {
            id = "config-1",
            gateStatus = ProjectGateStatus.SUCCEEDED,
            distributedSupply = 0
        };

        _allocationRepoMock
            .Setup(r => r.GetByIdAsync("alloc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _allocationRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _configRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tokenBalanceServiceMock
            .Setup(s => s.CreditAsync("user-1", "config-1", 100000, TokenHolderClass.FOUNDER, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto()));

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock
            .Setup(m => m.Map<TokenAllocationDto>(It.IsAny<TokenAllocation>()))
            .Returns(new TokenAllocationDto());

        // Act
        var result = await _sut.DistributeAsync("alloc-1", "user-1");

        // Assert
        result.IsSuccess.Should().BeTrue();

        _allocationRepoMock.Verify(r => r.UpdateAsync(It.Is<TokenAllocation>(a =>
            a.isLiquid == true // FOUNDER is liquid at SUCCEEDED
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // RevokeAllocationAsync — INVESTOR Allocation
    // ========================================================================

    [Fact]
    public async Task RevokeAllocationAsync_InvestorAllocation_DecrementsInvestorSupply()
    {
        // Arrange
        var allocation = new TokenAllocation
        {
            id = "alloc-1",
            projectTokenConfigId = "config-1",
            tokenAmount = 50000,
            status = AllocationStatus.RESERVED,
            holderClass = TokenHolderClass.INVESTOR
        };

        var config = new ProjectTokenConfig
        {
            id = "config-1",
            investorSupply = 200000,
            allocatedSupply = 500000
        };

        _allocationRepoMock
            .Setup(r => r.GetByIdAsync("alloc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _allocationRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _configRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock
            .Setup(m => m.Map<TokenAllocationDto>(It.IsAny<TokenAllocation>()))
            .Returns(new TokenAllocationDto { Status = AllocationStatus.REVOKED });

        // Act
        var result = await _sut.RevokeAllocationAsync("alloc-1");

        // Assert
        result.IsSuccess.Should().BeTrue();

        _configRepoMock.Verify(r => r.UpdateAsync(It.Is<ProjectTokenConfig>(c =>
            c.investorSupply == 150000 &&
            c.allocatedSupply == 450000
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // RevokeAllocationAsync — FOUNDER Allocation Does Not Decrement FounderSupply
    // ========================================================================

    [Fact]
    public async Task RevokeAllocationAsync_FounderAllocation_DoesNotDecrementFounderSupply()
    {
        // Arrange
        var allocation = new TokenAllocation
        {
            id = "alloc-1",
            projectTokenConfigId = "config-1",
            tokenAmount = 100000,
            status = AllocationStatus.RESERVED,
            holderClass = TokenHolderClass.FOUNDER
        };

        var config = new ProjectTokenConfig
        {
            id = "config-1",
            founderSupply = 200000,
            allocatedSupply = 500000
        };

        _allocationRepoMock
            .Setup(r => r.GetByIdAsync("alloc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _allocationRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _configRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock
            .Setup(m => m.Map<TokenAllocationDto>(It.IsAny<TokenAllocation>()))
            .Returns(new TokenAllocationDto { Status = AllocationStatus.REVOKED });

        // Act
        var result = await _sut.RevokeAllocationAsync("alloc-1");

        // Assert
        result.IsSuccess.Should().BeTrue();

        _configRepoMock.Verify(r => r.UpdateAsync(It.Is<ProjectTokenConfig>(c =>
            c.founderSupply == 200000 && // Not decremented
            c.allocatedSupply == 400000
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
