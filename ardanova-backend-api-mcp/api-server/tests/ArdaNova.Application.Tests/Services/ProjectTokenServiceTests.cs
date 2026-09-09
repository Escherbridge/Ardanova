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
using Microsoft.Extensions.Logging.Abstractions;
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
            _loggerMock.Object,
            new Mock<IAzoaValueEligibilityService>().Object);
    }

    /// <summary>
    /// Stamps the three node legs a project must have before any stake may be allocated:
    /// the minted asset, the treasury its funds land in, and the declared reserve partitions.
    /// Allocation is fail-closed until all three exist, so a fixture that omits them is
    /// refused before it ever reaches the rule under test. Partitions are declared at full
    /// supply so they are never the binding constraint in a test that is about something else.
    /// </summary>
    private static ProjectTokenConfig Provisioned(ProjectTokenConfig config)
    {
        config.status = ProjectTokenStatus.ACTIVE;
        config.assetId = "asset-1";
        config.treasuryAddress = "TREASURY-ADDRESS";
        config.partitionsDeclaredAt = DateTime.UtcNow;
        config.contributorPartitionSupply = config.totalSupply;
        config.investorPartitionSupply = config.totalSupply;
        return config;
    }

    [Fact]
    public async Task GetMetadataByIdsAsync_ProjectsOneQueryPreservesOrderAndReportsMissingIds()
    {
        var first = new ProjectTokenConfig
        {
            id = "config-1",
            projectId = "project-1",
            assetName = "First token",
            unitName = "FIRST"
        };
        var second = new ProjectTokenConfig
        {
            id = "config-2",
            projectId = "project-2",
            assetName = "Second token",
            unitName = "SECOND"
        };
        var configs = new[] { second, first };
        _configRepoMock
            .Setup(repository => repository.FindProjectedAsync(
                It.IsAny<Expression<Func<ProjectTokenConfig, bool>>>(),
                It.IsAny<Expression<Func<ProjectTokenConfig, ProjectTokenMetadataDto>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                Expression<Func<ProjectTokenConfig, bool>> predicate,
                Expression<Func<ProjectTokenConfig, ProjectTokenMetadataDto>> selector,
                CancellationToken _) => Task.FromResult<IReadOnlyList<ProjectTokenMetadataDto>>(
                    configs.AsQueryable().Where(predicate).Select(selector).ToArray()));

        var result = await _sut.GetMetadataByIdsAsync(
            ["config-1", "missing", "config-2", "config-1"]);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(config => config.Id).Should().Equal("config-1", "config-2");
        result.Value.MissingIds.Should().Equal("missing");
        _configRepoMock.Verify(repository => repository.FindProjectedAsync(
            It.IsAny<Expression<Func<ProjectTokenConfig, bool>>>(),
            It.IsAny<Expression<Func<ProjectTokenConfig, ProjectTokenMetadataDto>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMetadataByIdsAsync_RejectsUnboundedOrMalformedRequestsBeforeQuerying()
    {
        var empty = await _sut.GetMetadataByIdsAsync([]);
        var tooMany = await _sut.GetMetadataByIdsAsync(
            Enumerable.Repeat("duplicate", 501).ToArray());
        var oversized = await _sut.GetMetadataByIdsAsync([new string('x', 201)]);

        empty.Type.Should().Be(ResultType.ValidationError);
        tooMany.Type.Should().Be(ResultType.ValidationError);
        oversized.Type.Should().Be(ResultType.ValidationError);
        _configRepoMock.Verify(repository => repository.FindProjectedAsync(
            It.IsAny<Expression<Func<ProjectTokenConfig, bool>>>(),
            It.IsAny<Expression<Func<ProjectTokenConfig, ProjectTokenMetadataDto>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(19)]
    public async Task CreateConfigAsync_RejectsMissingOrUnsupportedAssetScaleBeforeWrites(int? assetScale)
    {
        var dto = new CreateProjectTokenConfigDto
        {
            ProjectId = "project-1",
            AssetName = "Test Token",
            UnitName = "TST",
            AssetScale = assetScale,
            TotalSupply = 1000000,
            FundingGoal = 50000,
        };

        var result = await _sut.CreateConfigAsync(dto);

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("Asset scale is required");
        _configRepoMock.Verify(repository => repository.AddAsync(
            It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
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
            AssetScale = 6,
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
            AssetScale = 6,
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
            AssetScale = 6,
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
    // AllocateToPbiAsync — Happy Path (ACTIVE Gate)
    // ========================================================================

    [Fact]
    public async Task AllocateToPbiAsync_ActiveGate_CreatesContributorAllocation()
    {
        // Arrange
        var config = Provisioned(new ProjectTokenConfig
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
        });

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
            PbiId = "task-1",
            EquityPercentage = 5
        };

        var allocationDto = new TokenAllocationDto
        {
            Id = "alloc-1",
            PbiId = "task-1",
            TokenAmount = 50000,
            HolderClass = TokenHolderClass.CONTRIBUTOR
        };

        _mapperMock
            .Setup(m => m.Map<TokenAllocationDto>(It.IsAny<TokenAllocation>()))
            .Returns(allocationDto);

        // Act
        var result = await _sut.AllocateToPbiAsync("config-1", dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.HolderClass.Should().Be(TokenHolderClass.CONTRIBUTOR);
        result.Value!.TokenAmount.Should().Be(50000);

        _allocationRepoMock.Verify(r => r.AddAsync(It.Is<TokenAllocation>(a =>
            a.pbiId == "task-1" &&
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
    // AllocateToPbiAsync — Gate Not ACTIVE
    // ========================================================================

    [Fact]
    public async Task AllocateToPbiAsync_GateNotActive_ReturnsValidationError()
    {
        // Arrange
        var config = Provisioned(new ProjectTokenConfig
        {
            id = "config-1",
            totalSupply = 1000000,
            gateStatus = ProjectGateStatus.FUNDING
        });

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var dto = new CreateTokenAllocationDto
        {
            PbiId = "task-1",
            EquityPercentage = 5
        };

        // Act
        var result = await _sut.AllocateToPbiAsync("config-1", dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("ACTIVE");
        result.Error.Should().Contain("FUNDING");

        _allocationRepoMock.Verify(r => r.AddAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ========================================================================
    // AllocateToPbiAsync — Insufficient Supply
    // ========================================================================

    [Fact]
    public async Task AllocateToPbiAsync_InsufficientSupply_ReturnsValidationError()
    {
        // Arrange
        var config = Provisioned(new ProjectTokenConfig
        {
            id = "config-1",
            totalSupply = 1000000,
            contributorSupply = 300000,
            investorSupply = 400000,
            founderSupply = 200000,
            burnedSupply = 50000,
            gateStatus = ProjectGateStatus.ACTIVE
        });

        _configRepoMock
            .Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var dto = new CreateTokenAllocationDto
        {
            PbiId = "task-1",
            EquityPercentage = 10 // 100,000 tokens, but only 50,000 available
        };

        // Act
        var result = await _sut.AllocateToPbiAsync("config-1", dto);

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
        var config = Provisioned(new ProjectTokenConfig
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
        });

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
        var config = Provisioned(new ProjectTokenConfig
        {
            id = "config-1",
            totalSupply = 1000000,
            contributorSupply = 100000,
            investorSupply = 200000,
            founderSupply = 200000,
            allocatedSupply = 500000
        });

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
        var config = Provisioned(new ProjectTokenConfig
        {
            id = "config-1",
            totalSupply = 1000000,
            founderSupply = 100000
        });

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
            a.isLiquid == true &&
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
    public async Task DistributeAsync_InvestorAtActive_IsLocked()
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
            a.isLiquid == false
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

    // ========================================================================
    // Worker awards (W7.2) — the two holds behind "you will be paid for this"
    // ========================================================================

    private static readonly Guid WorkerAvatarId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private const string EscrowHoldId = "award-bid-1";

    private Mock<IRepository<Project>> _awardProjectRepoMock = null!;
    private Mock<IRepository<TaskCommerceAgreement>> _awardAgreementRepoMock = null!;
    private Mock<IRepository<ProjectTask>> _awardTaskRepoMock = null!;
    private Mock<IAzoaCustodialAccountService> _awardAccountsMock = null!;
    private Mock<IAzoaHoldGateway> _awardHoldsMock = null!;

    /// <summary>
    /// A service wired for the award paths: the project, agreement and task it spans, the
    /// account read that names the worker on the node, and the node's own hold surface.
    /// </summary>
    private ProjectTokenService CreateAwardService()
    {
        _awardProjectRepoMock = new Mock<IRepository<Project>>();
        _awardAgreementRepoMock = new Mock<IRepository<TaskCommerceAgreement>>();
        _awardTaskRepoMock = new Mock<IRepository<ProjectTask>>();
        _awardAccountsMock = new Mock<IAzoaCustodialAccountService>();
        _awardHoldsMock = new Mock<IAzoaHoldGateway>();

        _awardAccountsMock
            .Setup(accounts => accounts.GetStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Success(new AzoaCustodialAccountStatusDto
            {
                AvatarId = WorkerAvatarId.ToString(),
                WalletId = "wallet-1",
                WalletAddress = "WORKER-ACCOUNT-ADDRESS",
                KycStatus = AzoaKycStatus.Approved,
                IdentityReady = true,
                KycReady = true,
                WalletReady = true,
                Ready = true,
            }));

        _unitOfWorkMock.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _allocationRepoMock
            .Setup(repository => repository.UpdateAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _allocationRepoMock
            .Setup(repository => repository.AddAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenAllocation allocation, CancellationToken _) => allocation);
        _configRepoMock
            .Setup(repository => repository.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mapperMock
            .Setup(mapper => mapper.Map<TokenAllocationDto>(It.IsAny<TokenAllocation>()))
            .Returns(new TokenAllocationDto { Id = "alloc-1" });

        return new ProjectTokenService(
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
            _loggerMock.Object,
            new AzoaValueEligibilityService(
                _awardAccountsMock.Object,
                NullLogger<AzoaValueEligibilityService>.Instance),
            _awardProjectRepoMock.Object,
            assets: null,
            ownerAccounts: _awardAccountsMock.Object,
            nodeGateways: null,
            configuration: null,
            holdGateways: new[] { _awardHoldsMock.Object },
            agreementRepo: _awardAgreementRepoMock.Object,
            taskRepo: _awardTaskRepoMock.Object);
    }

    private static AzoaHold Hold(
        string holdId,
        string state,
        string? parentHoldId = null,
        string? childHoldId = null)
        => new(
            holdId,
            "Algorand",
            "asset-1",
            "25000000",
            ProjectTokenService.ContributorPartitionName,
            WorkerAvatarId.ToString("N"),
            state,
            ReleaseNotBefore: null,
            AutoRelease: false,
            ParentHoldId: parentHoldId,
            ChildHoldId: childHoldId);

    private static ProjectTokenConfig AwardConfig(int contributorSupply = 0) => new()
    {
        id = "config-1",
        projectId = "project-1",
        assetId = "asset-1",
        assetChain = "Algorand",
        assetScale = 6,
        treasuryAddress = "TREASURY-ADDRESS",
        partitionsDeclaredAt = DateTime.UtcNow,
        status = ProjectTokenStatus.ACTIVE,
        gateStatus = ProjectGateStatus.ACTIVE,
        totalSupply = 1000,
        contributorPartitionSupply = 100,
        investorPartitionSupply = 100,
        contributorSupply = contributorSupply,
    };

    private static ProjectAwardReservationRequest AwardRequest(decimal amount = 25m, string holdId = EscrowHoldId)
        => new("config-1", "agreement-1", holdId, amount, "worker-1", WorkerAvatarId);

    [Fact]
    public async Task ReserveWorkAwardAsync_CommitsTheAwardOnTheNodeAndDrawsTheAllowance()
    {
        var service = CreateAwardService();
        var config = AwardConfig();
        _configRepoMock.Setup(repository => repository.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _allocationRepoMock.Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<TokenAllocation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenAllocation?)null);
        _awardHoldsMock.Setup(holds => holds.OpenAsync(It.IsAny<AzoaHoldOpenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaHold>.Success(Hold(EscrowHoldId, AzoaHoldStates.Held)));
        TokenAllocation? created = null;
        _allocationRepoMock.Setup(repository => repository.AddAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()))
            .Callback<TokenAllocation, CancellationToken>((allocation, _) => created = allocation)
            .ReturnsAsync((TokenAllocation allocation, CancellationToken _) => allocation);

        var result = await service.ReserveWorkAwardAsync(AwardRequest());

        result.IsSuccess.Should().BeTrue();
        result.Value!.HoldId.Should().Be(EscrowHoldId);
        result.Value.WholeUnits.Should().Be(25);

        // The escrow hold must never auto-release: a hold with no window that asked to be swept
        // would pay the worker before the owner ever approved the work.
        _awardHoldsMock.Verify(holds => holds.OpenAsync(
            It.Is<AzoaHoldOpenRequest>(request =>
                request.Reference.HoldId == EscrowHoldId
                && request.AssetId == "asset-1"
                && request.AmountBaseUnits == "25000000"
                && request.ReservePartition == ProjectTokenService.ContributorPartitionName
                && request.BeneficiaryAvatarId == WorkerAvatarId
                && request.ReleaseNotBefore == null
                && request.AutoRelease == false),
            It.IsAny<CancellationToken>()), Times.Once);

        created.Should().NotBeNull();
        created!.holdId.Should().Be(EscrowHoldId);
        created.holdState.Should().Be(AzoaHoldStates.Held);
        created.IsNodeHeld.Should().BeTrue();
        created.recipientUserId.Should().Be("worker-1");
        created.taskCommerceAgreementId.Should().Be("agreement-1");
        created.tokenAmount.Should().Be(25);
        config.contributorSupply.Should().Be(25);
        config.allocatedSupply.Should().Be(25);
    }

    [Fact]
    public async Task ReserveWorkAwardAsync_NodeRefusalLeavesNoAllocationAndNoSupplyChange()
    {
        var service = CreateAwardService();
        var config = AwardConfig();
        _configRepoMock.Setup(repository => repository.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _allocationRepoMock.Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<TokenAllocation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenAllocation?)null);
        _awardHoldsMock.Setup(holds => holds.OpenAsync(It.IsAny<AzoaHoldOpenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaHold>.ValidationError(
                "Partition 'contributor' would be oversubscribed: 25000000 base units requested, 0 available."));

        var result = await service.ReserveWorkAwardAsync(AwardRequest());

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("oversubscribed");
        config.contributorSupply.Should().Be(0);
        _allocationRepoMock.Verify(
            repository => repository.AddAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReserveWorkAwardAsync_HoldThatIsNotHeldIsNeverRecordedAsFunded()
    {
        var service = CreateAwardService();
        _configRepoMock.Setup(repository => repository.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AwardConfig());
        _allocationRepoMock.Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<TokenAllocation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenAllocation?)null);
        _awardHoldsMock.Setup(holds => holds.OpenAsync(It.IsAny<AzoaHoldOpenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaHold>.Success(Hold(EscrowHoldId, AzoaHoldStates.Refunded)));

        var result = await service.ReserveWorkAwardAsync(AwardRequest());

        result.IsSuccess.Should().BeFalse();
        _allocationRepoMock.Verify(
            repository => repository.AddAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReserveWorkAwardAsync_UnfundableProjectIsRefusedBeforeTheNodeIsCalled()
    {
        var service = CreateAwardService();
        _configRepoMock.Setup(repository => repository.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectTokenConfig { id = "config-1", projectId = "project-1", assetScale = 6 });

        var result = await service.ReserveWorkAwardAsync(AwardRequest());

        result.IsSuccess.Should().BeFalse();
        _awardHoldsMock.Verify(
            holds => holds.OpenAsync(It.IsAny<AzoaHoldOpenRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReserveWorkAwardAsync_PartUnitAwardIsRefusedBeforeTheNodeIsCalled()
    {
        var service = CreateAwardService();
        _configRepoMock.Setup(repository => repository.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AwardConfig());
        _allocationRepoMock.Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<TokenAllocation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenAllocation?)null);

        var result = await service.ReserveWorkAwardAsync(AwardRequest(amount: 25.5m));

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("whole number of units");
        _awardHoldsMock.Verify(
            holds => holds.OpenAsync(It.IsAny<AzoaHoldOpenRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReserveWorkAwardAsync_LocallyOverdrawnAllowanceIsRefusedBeforeTheNodeIsCalled()
    {
        var service = CreateAwardService();
        _configRepoMock.Setup(repository => repository.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AwardConfig(contributorSupply: 90));
        _allocationRepoMock.Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<TokenAllocation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenAllocation?)null);

        var result = await service.ReserveWorkAwardAsync(AwardRequest());

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("oversubscribed");
        _awardHoldsMock.Verify(
            holds => holds.OpenAsync(It.IsAny<AzoaHoldOpenRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReserveWorkAwardAsync_ReplaysAnExistingHoldWithoutDrawingTheAllowanceTwice()
    {
        var service = CreateAwardService();
        var config = AwardConfig(contributorSupply: 25);
        _configRepoMock.Setup(repository => repository.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _allocationRepoMock.Setup(repository => repository.FindOneAsync(
                It.IsAny<Expression<Func<TokenAllocation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenAllocation
            {
                id = "alloc-1",
                projectTokenConfigId = "config-1",
                holdId = EscrowHoldId,
                holdState = AzoaHoldStates.Held,
                tokenAmount = 25,
            });

        var result = await service.ReserveWorkAwardAsync(AwardRequest());

        result.IsSuccess.Should().BeTrue();
        result.Value!.AllocationId.Should().Be("alloc-1");
        config.contributorSupply.Should().Be(25);
        _awardHoldsMock.Verify(
            holds => holds.OpenAsync(It.IsAny<AzoaHoldOpenRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _allocationRepoMock.Verify(
            repository => repository.AddAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApproveWorkCompletionAsync_ContinuesTheEscrowHoldIntoTheWorkersWindow()
    {
        var service = CreateAwardService();
        var allocation = HeldAllocation();
        var agreement = AwardAgreement();
        var task = new ProjectTask { id = "task-1", projectId = "project-1", escrowStatus = EscrowStatus.FUNDED };
        ArrangeAwardLookups(allocation, agreement, task);
        _awardHoldsMock.Setup(holds => holds.ReleaseIntoHoldAsync(
                It.IsAny<AzoaHoldChainRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaHold>.Success(
                Hold(EscrowHoldId, AzoaHoldStates.ReleasedIntoHold, childHoldId: EscrowHoldId + "-window")));
        _awardHoldsMock.Setup(holds => holds.GetAsync(It.IsAny<AzoaHoldReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaHold>.Success(
                Hold(EscrowHoldId + "-window", AzoaHoldStates.Held, parentHoldId: EscrowHoldId)));
        _tokenBalanceServiceMock
            .Setup(balances => balances.CreditAsync(
                "worker-1", "config-1", 25, TokenHolderClass.CONTRIBUTOR, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto()));

        var result = await service.ApproveWorkCompletionAsync("alloc-1", "owner-1");

        result.IsSuccess.Should().BeTrue();
        _awardHoldsMock.Verify(holds => holds.ReleaseIntoHoldAsync(
            It.Is<AzoaHoldChainRequest>(request =>
                request.Reference.HoldId == EscrowHoldId
                && request.NextHoldId == EscrowHoldId + "-window"
                && request.NextBeneficiaryAvatarId == WorkerAvatarId
                && request.ReleaseNotBefore != null
                && request.AutoRelease),
            It.IsAny<CancellationToken>()), Times.Once);

        allocation.status.Should().Be(AllocationStatus.DISTRIBUTED);
        allocation.workerHoldId.Should().Be(EscrowHoldId + "-window");
        allocation.workerHoldState.Should().Be(AzoaHoldStates.Held);
        allocation.workerHoldReleasesAt.Should().NotBeNull();
        allocation.workerHoldReleasesAt!.Value.Should().BeAfter(DateTime.UtcNow.AddDays(89));
        // Restricted until the window elapses: the award is theirs and not yet liquid.
        allocation.isLiquid.Should().BeFalse();
        agreement.status.Should().Be(TaskCommerceAgreementStatus.SETTLED);
        task.escrowStatus.Should().Be(EscrowStatus.RELEASED);
    }

    // -- A declared guild share is a beneficiary, not a stranded hold --

    [Fact]
    public async Task ApproveWorkCompletionAsync_SettlesEveryLegOfASplitAward_AndCreditsEachRecipient()
    {
        // An accepted bid with a guild share is held in TWO legs against one agreement.
        // Approval used to refuse any leg whose recipient was not the agreement's contributor
        // while marking the agreement SETTLED anyway, so the guild's units were drawn from the
        // project and could never be handed over or returned.
        var service = CreateAwardService();
        var workerLeg = HeldAllocation();
        var guildLeg = GuildAllocation();
        var agreement = AwardAgreement();
        var task = new ProjectTask { id = "task-1", projectId = "project-1", escrowStatus = EscrowStatus.FUNDED };
        ArrangeAwardLookups(workerLeg, agreement, task);
        _allocationRepoMock.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<TokenAllocation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TokenAllocation> { workerLeg, guildLeg });
        _awardHoldsMock.Setup(holds => holds.ReleaseIntoHoldAsync(
                It.IsAny<AzoaHoldChainRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AzoaHoldChainRequest request, CancellationToken _) =>
                Result<AzoaHold>.Success(Hold(
                    request.Reference.HoldId,
                    AzoaHoldStates.ReleasedIntoHold,
                    childHoldId: request.NextHoldId)));
        _awardHoldsMock.Setup(holds => holds.GetAsync(It.IsAny<AzoaHoldReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaHold>.Success(Hold(EscrowHoldId, AzoaHoldStates.Held)));
        var credited = new List<(string User, int Units)>();
        _tokenBalanceServiceMock
            .Setup(balances => balances.CreditAsync(
                It.IsAny<string>(), "config-1", It.IsAny<int>(),
                It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, int, TokenHolderClass, CancellationToken>(
                (user, _, units, _, _) => credited.Add((user, units)))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto()));

        var result = await service.ApproveWorkCompletionAsync("alloc-1", "owner-1");

        result.IsSuccess.Should().BeTrue();

        // BOTH legs left RESERVED, and each recipient was credited their OWN share - crediting
        // the agreement's contributor for the whole award is the other way to get this wrong.
        workerLeg.status.Should().Be(AllocationStatus.DISTRIBUTED);
        guildLeg.status.Should().Be(AllocationStatus.DISTRIBUTED);
        credited.Should().BeEquivalentTo(new[] { ("worker-1", 25), ("guild-owner-1", 5) });

        _awardHoldsMock.Verify(holds => holds.ReleaseIntoHoldAsync(
            It.Is<AzoaHoldChainRequest>(request => request.Reference.HoldId == EscrowHoldId),
            It.IsAny<CancellationToken>()), Times.Once);
        _awardHoldsMock.Verify(holds => holds.ReleaseIntoHoldAsync(
            It.Is<AzoaHoldChainRequest>(request => request.Reference.HoldId == GuildHoldId),
            It.IsAny<CancellationToken>()), Times.Once);

        agreement.status.Should().Be(TaskCommerceAgreementStatus.SETTLED);
        task.escrowStatus.Should().Be(EscrowStatus.RELEASED);
    }

    [Fact]
    public async Task ApproveWorkCompletionAsync_WhenOneLegCannotBeSettled_NothingIsMarkedSettled()
    {
        // A partial settlement must not report a finished assignment. The agreement and the
        // task stay where they were, which is what is true: some of the award is still held.
        var service = CreateAwardService();
        var workerLeg = HeldAllocation();
        var guildLeg = GuildAllocation();
        guildLeg.holdId = null;
        var agreement = AwardAgreement();
        var task = new ProjectTask { id = "task-1", projectId = "project-1", escrowStatus = EscrowStatus.FUNDED };
        ArrangeAwardLookups(workerLeg, agreement, task);
        _allocationRepoMock.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<TokenAllocation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TokenAllocation> { workerLeg, guildLeg });
        _awardHoldsMock.Setup(holds => holds.ReleaseIntoHoldAsync(
                It.IsAny<AzoaHoldChainRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AzoaHoldChainRequest request, CancellationToken _) =>
                Result<AzoaHold>.Success(Hold(
                    request.Reference.HoldId,
                    AzoaHoldStates.ReleasedIntoHold,
                    childHoldId: request.NextHoldId)));
        _awardHoldsMock.Setup(holds => holds.GetAsync(It.IsAny<AzoaHoldReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaHold>.Success(Hold(EscrowHoldId, AzoaHoldStates.Held)));
        _tokenBalanceServiceMock
            .Setup(balances => balances.CreditAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto()));

        var result = await service.ApproveWorkCompletionAsync("alloc-1", "owner-1");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("never set aside on the shared node");
        agreement.status.Should().Be(TaskCommerceAgreementStatus.ESCROW_FUNDED);
        task.escrowStatus.Should().Be(EscrowStatus.FUNDED);
    }

    [Fact]
    public async Task ApproveWorkCompletionAsync_ALegSetWithoutTheWorkersOwnShareIsRefused()
    {
        // The worker's leg is what makes the set this assignment's. Without it, nothing here
        // is evidence that the units belong to this agreement at all.
        var service = CreateAwardService();
        var guildLeg = GuildAllocation();
        var agreement = AwardAgreement();
        ArrangeAwardLookups(HeldAllocation(), agreement, new ProjectTask { id = "task-1", projectId = "project-1" });
        _allocationRepoMock.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<TokenAllocation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TokenAllocation> { guildLeg });

        var result = await service.ApproveWorkCompletionAsync("alloc-1", "owner-1");

        result.Type.Should().Be(ResultType.Conflict);
        _awardHoldsMock.Verify(
            holds => holds.ReleaseIntoHoldAsync(It.IsAny<AzoaHoldChainRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApproveWorkCompletionAsync_SomeoneOtherThanTheProjectOwnerIsRefused()
    {
        var service = CreateAwardService();
        ArrangeAwardLookups(HeldAllocation(), AwardAgreement(), new ProjectTask { id = "task-1", projectId = "project-1" });

        var result = await service.ApproveWorkCompletionAsync("alloc-1", "someone-else");

        result.Type.Should().Be(ResultType.Forbidden);
        _awardHoldsMock.Verify(
            holds => holds.ReleaseIntoHoldAsync(It.IsAny<AzoaHoldChainRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApproveWorkCompletionAsync_AnAwardTheNodeNeverHeldCannotBePaidOut()
    {
        var service = CreateAwardService();
        var reservation = HeldAllocation();
        reservation.holdId = null;
        _allocationRepoMock.Setup(repository => repository.GetByIdAsync("alloc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var result = await service.ApproveWorkCompletionAsync("alloc-1", "owner-1");

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("never set aside on the shared node");
        _awardHoldsMock.Verify(
            holds => holds.ReleaseIntoHoldAsync(It.IsAny<AzoaHoldChainRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApproveWorkCompletionAsync_IsIdempotentOnceTheWindowExists()
    {
        var service = CreateAwardService();
        var allocation = HeldAllocation();
        allocation.status = AllocationStatus.DISTRIBUTED;
        allocation.workerHoldId = EscrowHoldId + "-window";
        ArrangeAwardLookups(allocation, AwardAgreement(), new ProjectTask { id = "task-1", projectId = "project-1" });

        var result = await service.ApproveWorkCompletionAsync("alloc-1", "owner-1");

        result.IsSuccess.Should().BeTrue();
        _awardHoldsMock.Verify(
            holds => holds.ReleaseIntoHoldAsync(It.IsAny<AzoaHoldChainRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DistributeAsync_AnAwardTheNodeIsHoldingCannotBeHandedOverDirectly()
    {
        var service = CreateAwardService();
        _allocationRepoMock.Setup(repository => repository.GetByIdAsync("alloc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(HeldAllocation());

        var result = await service.DistributeAsync("alloc-1", "worker-1");

        result.Type.Should().Be(ResultType.Conflict);
        _tokenBalanceServiceMock.Verify(balances => balances.CreditAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DistributeAsync_CannotRedirectAnAllocationBoundToSomeoneElse()
    {
        var service = CreateAwardService();
        var bound = HeldAllocation();
        bound.holdId = null;
        _allocationRepoMock.Setup(repository => repository.GetByIdAsync("alloc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(bound);

        var result = await service.DistributeAsync("alloc-1", "someone-else");

        result.Type.Should().Be(ResultType.Forbidden);
        _tokenBalanceServiceMock.Verify(balances => balances.CreditAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeAllocationAsync_GivesTheNodeCommitmentBackBeforeFreeingTheLocalCount()
    {
        var service = CreateAwardService();
        var allocation = HeldAllocation();
        var config = AwardConfig(contributorSupply: 25);
        config.allocatedSupply = 25;
        _allocationRepoMock.Setup(repository => repository.GetByIdAsync("alloc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);
        _configRepoMock.Setup(repository => repository.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _awardHoldsMock.Setup(holds => holds.RefundAsync(
                It.IsAny<AzoaHoldReference>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaHold>.Success(Hold(EscrowHoldId, AzoaHoldStates.Refunded)));

        var result = await service.RevokeAllocationAsync("alloc-1");

        result.IsSuccess.Should().BeTrue();
        _awardHoldsMock.Verify(holds => holds.RefundAsync(
            It.Is<AzoaHoldReference>(reference => reference.HoldId == EscrowHoldId),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
        allocation.status.Should().Be(AllocationStatus.REVOKED);
        allocation.holdState.Should().Be(AzoaHoldStates.Refunded);
        config.contributorSupply.Should().Be(0);
    }

    [Fact]
    public async Task RevokeAllocationAsync_RefusesOnceTheWorkersWindowExists()
    {
        var service = CreateAwardService();
        var allocation = HeldAllocation();
        allocation.workerHoldId = EscrowHoldId + "-window";
        _allocationRepoMock.Setup(repository => repository.GetByIdAsync("alloc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);

        var result = await service.RevokeAllocationAsync("alloc-1");

        result.Type.Should().Be(ResultType.Conflict);
        _awardHoldsMock.Verify(holds => holds.RefundAsync(
            It.IsAny<AzoaHoldReference>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static TokenAllocation HeldAllocation() => new()
    {
        id = "alloc-1",
        projectTokenConfigId = "config-1",
        recipientUserId = "worker-1",
        taskCommerceAgreementId = "agreement-1",
        tokenAmount = 25,
        status = AllocationStatus.RESERVED,
        holderClass = TokenHolderClass.CONTRIBUTOR,
        isLiquid = false,
        holdChain = "Algorand",
        reservePartition = ProjectTokenService.ContributorPartitionName,
        holdId = EscrowHoldId,
        holdState = AzoaHoldStates.Held,
    };

    private const string GuildHoldId = EscrowHoldId + "-guild";

    /// <summary>The guild's leg of a split award: same agreement, a different beneficiary.</summary>
    private static TokenAllocation GuildAllocation() => new()
    {
        id = "alloc-2",
        projectTokenConfigId = "config-1",
        recipientUserId = "guild-owner-1",
        taskCommerceAgreementId = "agreement-1",
        tokenAmount = 5,
        status = AllocationStatus.RESERVED,
        holderClass = TokenHolderClass.CONTRIBUTOR,
        isLiquid = false,
        holdChain = "Algorand",
        reservePartition = ProjectTokenService.ContributorPartitionName,
        holdId = GuildHoldId,
        holdState = AzoaHoldStates.Held,
    };

    private static TaskCommerceAgreement AwardAgreement() => new()
    {
        id = "agreement-1",
        projectId = "project-1",
        taskId = "task-1",
        bidId = "bid-1",
        contributorUserId = "worker-1",
        projectTokenConfigId = "config-1",
        awardAmount = 25m,
        status = TaskCommerceAgreementStatus.ESCROW_FUNDED,
    };

    private void ArrangeAwardLookups(
        TokenAllocation allocation,
        TaskCommerceAgreement agreement,
        ProjectTask task)
    {
        _allocationRepoMock.Setup(repository => repository.GetByIdAsync("alloc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);
        // Approval settles every leg of the agreement, so it reads the whole set, not one row.
        _allocationRepoMock.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<TokenAllocation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TokenAllocation> { allocation });
        _configRepoMock.Setup(repository => repository.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AwardConfig(contributorSupply: 25));
        _awardProjectRepoMock.Setup(repository => repository.GetByIdAsync("project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { id = "project-1", createdById = "owner-1" });
        _awardAgreementRepoMock.Setup(repository => repository.GetByIdAsync("agreement-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(agreement);
        _awardTaskRepoMock.Setup(repository => repository.GetByIdAsync("task-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _awardTaskRepoMock.Setup(repository => repository.UpdateAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _awardAgreementRepoMock.Setup(repository => repository.UpdateAsync(
                It.IsAny<TaskCommerceAgreement>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}
