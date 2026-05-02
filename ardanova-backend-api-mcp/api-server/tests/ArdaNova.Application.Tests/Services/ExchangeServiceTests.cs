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

public class ExchangeServiceTests
{
    private readonly Mock<IRepository<ProjectTokenConfig>> _projectTokenConfigRepo;
    private readonly Mock<IRepository<PlatformTreasury>> _treasuryRepo;
    private readonly Mock<IMapper> _mapper;
    private readonly ExchangeService _sut;

    public ExchangeServiceTests()
    {
        _projectTokenConfigRepo = new Mock<IRepository<ProjectTokenConfig>>();
        _treasuryRepo = new Mock<IRepository<PlatformTreasury>>();
        _mapper = new Mock<IMapper>();
        _sut = new ExchangeService(
            _projectTokenConfigRepo.Object,
            _treasuryRepo.Object,
            _mapper.Object
        );
    }

    [Fact]
    public async Task GetProjectTokenValueAsync_ValidConfig_ReturnsValue()
    {
        // Arrange
        var projectTokenConfigId = "ptc1";
        var projectTokenConfig = new ProjectTokenConfig
        {
            id = projectTokenConfigId,
            fundingRaised = 10000.0,
            totalSupply = 1000,
            gateStatus = ProjectGateStatus.ACTIVE
        };

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(projectTokenConfigId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectTokenConfig);

        // Act
        var result = await _sut.GetProjectTokenValueAsync(projectTokenConfigId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(10.0);
    }

    [Fact]
    public async Task GetProjectTokenValueAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var projectTokenConfigId = "ptc1";

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(projectTokenConfigId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTokenConfig?)null);

        // Act
        var result = await _sut.GetProjectTokenValueAsync(projectTokenConfigId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task GetProjectTokenValueAsync_ZeroSupply_ReturnsFailure()
    {
        // Arrange
        var projectTokenConfigId = "ptc1";
        var projectTokenConfig = new ProjectTokenConfig
        {
            id = projectTokenConfigId,
            fundingRaised = 10000.0,
            totalSupply = 0,
            gateStatus = ProjectGateStatus.ACTIVE
        };

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(projectTokenConfigId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectTokenConfig);

        // Act
        var result = await _sut.GetProjectTokenValueAsync(projectTokenConfigId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("zero");
    }

    [Fact]
    public async Task GetArdaValueAsync_ValidTreasury_ReturnsValue()
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

        _treasuryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformTreasury> { treasury });

        // Act
        var result = await _sut.GetArdaValueAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // (6000 + 3000 + 1000) / 5000 = 2.0
        result.Value.Should().Be(2.0);
    }

    [Fact]
    public async Task GetArdaValueAsync_NoTreasury_ReturnsFailure()
    {
        // Arrange
        _treasuryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformTreasury>());

        // Act
        var result = await _sut.GetArdaValueAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task GetArdaValueAsync_ZeroCirculating_ReturnsFailure()
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
            ardaCirculatingSupply = 0,
            lastReconciliationAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _treasuryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformTreasury> { treasury });

        // Act
        var result = await _sut.GetArdaValueAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("zero");
    }

    [Fact]
    public async Task CalculateConversionAsync_Valid_ReturnsPreview()
    {
        // Arrange
        var projectTokenConfigId = "ptc1";
        var tokenAmount = 100;

        var projectTokenConfig = new ProjectTokenConfig
        {
            id = projectTokenConfigId,
            fundingRaised = 10000.0,
            totalSupply = 1000,
            gateStatus = ProjectGateStatus.ACTIVE
        };

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

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(projectTokenConfigId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectTokenConfig);

        _treasuryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformTreasury> { treasury });

        var expectedDto = new ConversionPreviewDto
        {
            ProjectTokenValueUsd = 10.0,
            ArdaValueUsd = 2.0,
            SourceTokenAmount = tokenAmount,
            UsdValue = 1000.0,
            ArdaAmount = 500
        };

        _mapper.Setup(m => m.Map<ConversionPreviewDto>(It.IsAny<object>()))
            .Returns(expectedDto);

        // Act
        var result = await _sut.CalculateConversionAsync(projectTokenConfigId, tokenAmount, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.SourceTokenAmount.Should().Be(tokenAmount);
        // projectTokenValue = 10000 / 1000 = 10
        // usdValue = 100 * 10 = 1000
        // ardaValue = (6000 + 3000 + 1000) / 5000 = 2
        // ardaAmount = 1000 / 2 = 500
        result.Value.UsdValue.Should().Be(1000.0);
        result.Value.ArdaAmount.Should().Be(500);
    }

    [Fact]
    public async Task CalculateConversionAsync_ZeroAmount_ReturnsFailure()
    {
        // Arrange
        var projectTokenConfigId = "ptc1";
        var tokenAmount = 0;

        // Act
        var result = await _sut.CalculateConversionAsync(projectTokenConfigId, tokenAmount, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("must be positive");
    }
}
