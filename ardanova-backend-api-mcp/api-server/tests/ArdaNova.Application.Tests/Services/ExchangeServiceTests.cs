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

    [Fact]
    public async Task CalculateConversionAsync_PricesInArdaInOneHop()
    {
        // Arrange: one unit is worth $10, one ARDA is worth $2, so a unit costs 5 ARDA.
        ArrangeRate(fundingRaised: 10_000, totalSupply: 1_000, backing: 10_000, circulating: 5_000);

        // Act
        var result = await _sut.CalculateConversionAsync("ptc1", 100, CancellationToken.None);

        // Assert: the exact fields are the contract; the doubles are only their projection.
        result.IsSuccess.Should().BeTrue();
        result.Value!.ArdaPerProjectUnit.Should().Be(5m);
        result.Value.ArdaAmountExact.Should().Be(500m);
        result.Value.ArdaMinorUnits.Should().Be(500_00);
        result.Value.RateSource.Should().Be(ExchangeService.DeclaredRateSource);
    }

    [Fact]
    public async Task GetDeclaredRateAsync_DeclaresArdaPerUnitAndTheUsdReference()
    {
        // Arrange
        ArrangeRate(fundingRaised: 10_000, totalSupply: 1_000, backing: 10_000, circulating: 5_000);

        // Act
        var result = await _sut.GetDeclaredRateAsync("ptc1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ArdaPerWholeProjectUnit.Should().Be("5");
        result.Value.UsdMajorUnitsPerWholeProjectUnit.Should().Be("10");
        result.Value.Source.Should().Be(ExchangeService.DeclaredRateSource);
    }

    private void ArrangeRate(double fundingRaised, int totalSupply, double backing, long circulating)
    {
        _projectTokenConfigRepo
            .Setup(r => r.GetByIdAsync("ptc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectTokenConfig
            {
                id = "ptc1",
                assetScale = 2,
                totalSupply = totalSupply,
                fundingRaised = fundingRaised,
            });

        _treasuryRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformTreasury>
            {
                new()
                {
                    id = "treasury1",
                    indexFundBalance = backing,
                    liquidReserveBalance = 0,
                    operationsBalance = 0,
                    ardaCirculatingSupply = circulating,
                }
            });
    }
}

/// <summary>
/// The money primitive every value path goes through: exact minor units, refusals instead of
/// silent rounding, and no binary floating point anywhere in the arithmetic.
/// </summary>
public class MoneyTests
{
    [Theory]
    [InlineData("0.01", 1L)]
    [InlineData("1", 100L)]
    [InlineData("1234.56", 123456L)]
    public void TryToMinorUnits_ConvertsExactAmounts(string amount, long expected)
    {
        Money.TryToMinorUnits(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), out var minorUnits)
            .Should().BeTrue();
        minorUnits.Should().Be(expected);
    }

    [Fact]
    public void TryToMinorUnits_RefusesSubCentPrecisionRatherThanRoundingItAway()
    {
        // A third of a cent is not money this platform can hold, and quietly dropping it is
        // exactly the defect the fixed-scale model exists to remove.
        Money.TryToMinorUnits(0.005m, out var minorUnits).Should().BeFalse();
        minorUnits.Should().Be(0);
    }

    [Fact]
    public void TryToMinorUnits_RefusesAmountsThatCannotBeRepresented()
    {
        Money.TryToMinorUnits(decimal.MaxValue, out _).Should().BeFalse();
    }

    [Fact]
    public void TryToMinorUnitsFloor_DropsTheSurplusInsteadOfRoundingUp()
    {
        Money.TryToMinorUnitsFloor(1.999m, out var minorUnits).Should().BeTrue();
        minorUnits.Should().Be(199);
    }

    [Fact]
    public void TryToWholeUnitsFloor_NeverInventsAPartialUnit()
    {
        Money.TryToWholeUnitsFloor(9.99m, out var wholeUnits).Should().BeTrue();
        wholeUnits.Should().Be(9);
    }

    [Fact]
    public void FromMinorUnits_RoundTripsExactly()
    {
        Money.FromMinorUnits(123456).Should().Be(1234.56m);
    }

    [Fact]
    public void ToExactString_IsInvariantAndUnrounded()
    {
        Money.ToExactString(0.1m + 0.2m).Should().Be("0.3");
    }
}
