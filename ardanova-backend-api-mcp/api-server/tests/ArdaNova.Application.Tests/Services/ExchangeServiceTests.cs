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

    // -- The first sponsorship: a project nobody has backed yet still has a price (OI-89b) --

    [Fact]
    public async Task GetProjectTokenValueAsync_NobodyHasSponsoredYet_QuotesTheOpeningOffer()
    {
        // Nothing raised, so there is no backer-set price. The opening offer the founder already
        // named - $50,000 across 10,000 units - is $5 a unit.
        ArrangeProject(fundingRaised: 0, fundingGoal: 50_000, totalSupply: 10_000);

        var result = await _sut.GetProjectTokenValueAsync("ptc1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(5.0);
    }

    [Fact]
    public async Task GetDeclaredRateAsync_NobodyHasSponsoredYet_PricesTheOfferAndMarksItAsSuch()
    {
        // $5 a unit at the opening offer and one ARDA worth $2, so a unit costs 2.5 ARDA. This
        // rate was 0 before, which is why the first sponsorship of any project was unpriceable.
        ArrangeRate(fundingRaised: 0, totalSupply: 10_000, backing: 10_000, circulating: 5_000, fundingGoal: 50_000);

        var result = await _sut.GetDeclaredRateAsync("ptc1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ArdaPerWholeProjectUnit.Should().Be("2.5");
        result.Value.UsdMajorUnitsPerWholeProjectUnit.Should().Be("5");
        // The marker the receipt carries, so a sponsor is told which of the two prices they got.
        result.Value.Source.Should().Be(ExchangeService.OfferingRateSource);
    }

    [Fact]
    public async Task GetDeclaredRateAsync_OnceTheAskIsMet_TheBackerSetPriceReplacesTheOffer()
    {
        // The ask was $50,000 and backers have put in $100,000, so the raise is over and the
        // price is the one they set: $10 a unit. The handover is at the ASK BEING MET, not at
        // the first sponsorship - see the collapse test below for why.
        ArrangeRate(fundingRaised: 100_000, totalSupply: 10_000, backing: 10_000, circulating: 5_000, fundingGoal: 50_000);

        var result = await _sut.GetDeclaredRateAsync("ptc1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UsdMajorUnitsPerWholeProjectUnit.Should().Be("10");
        result.Value.Source.Should().Be(ExchangeService.DeclaredRateSource);
    }

    [Fact]
    public async Task CalculateConversionAsync_NobodyHasSponsoredYet_PreviewsAtTheOpeningOffer()
    {
        ArrangeRate(fundingRaised: 0, totalSupply: 10_000, backing: 10_000, circulating: 5_000, fundingGoal: 50_000);

        var result = await _sut.CalculateConversionAsync("ptc1", 100, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ArdaPerProjectUnit.Should().Be(2.5m);
        result.Value.ArdaAmountExact.Should().Be(250m);
        result.Value.RateSource.Should().Be(ExchangeService.OfferingRateSource);
    }

    [Fact]
    public async Task GetProjectTokenValueAsync_NoSponsorshipAndNoAmountToRaise_RefusesByName()
    {
        // Nothing raised and nothing asked for is not a price of zero; it is no price at all.
        ArrangeProject(fundingRaised: 0, fundingGoal: 0, totalSupply: 10_000);

        var result = await _sut.GetProjectTokenValueAsync("ptc1", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("This project has not set an amount to raise, so it has no opening price yet");
    }

    [Fact]
    public async Task GetProjectTokenValueAsync_FundingTotalBelowZero_RefusesRatherThanFallingBackToTheOffer()
    {
        // A below-zero total is a broken figure, not an unsponsored project. Quoting the opening
        // offer here would price it anyway and hide the defect.
        ArrangeProject(fundingRaised: -1, fundingGoal: 50_000, totalSupply: 10_000);

        var result = await _sut.GetProjectTokenValueAsync("ptc1", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("This project's funding total is below zero, so no price can be quoted");
    }

    // -- The genesis treasury: a rate of zero is a refusal, not a division (OI-34) --

    [Fact]
    public async Task GetArdaValueAsync_GenesisTreasury_RefusesByNameInsteadOfReturningZero()
    {
        // Three bucket balances still at 0 with ARDA already issued. The rate divides out to 0,
        // and 0 is precisely what every caller of this then divides by.
        ArrangeTreasury(backing: 0, circulating: 10_000_000);

        var result = await _sut.GetArdaValueAsync(CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("ARDA has no backing, so no rate can be quoted");
    }

    [Fact]
    public async Task GetDeclaredRateAsync_GenesisTreasury_RefusesInsteadOfThrowing()
    {
        // One of the two division sites. Without the guard this raised DivideByZeroException and
        // the route answered 500, so the test would fail on the exception, not the assertion.
        ArrangeRate(fundingRaised: 10_000, totalSupply: 1_000, backing: 0, circulating: 10_000_000);

        var result = await _sut.GetDeclaredRateAsync("ptc1", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("ARDA has no backing, so no rate can be quoted");
    }

    [Fact]
    public async Task CalculateConversionAsync_GenesisTreasury_RefusesInsteadOfThrowing()
    {
        // The other division site, reached by the preview a sponsor sees before committing.
        ArrangeRate(fundingRaised: 10_000, totalSupply: 1_000, backing: 0, circulating: 10_000_000);

        var result = await _sut.CalculateConversionAsync("ptc1", 100, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("ARDA has no backing, so no rate can be quoted");
    }

    // -- The unit the ask is denominated in, and the price that must not collapse (OI-89b) --

    [Fact]
    public async Task GetDeclaredRateAsync_TheOpeningOfferDividesADollarAsk_NotAPlatformUnitOne()
    {
        // The worked example from the defect. A founder asks for $5,000 across 500,000 units,
        // and one ARDA is worth $0.01, so a unit costs exactly ONE ARDA. When the ask was stored
        // as the founder's platform-unit figure (500,000) and divided into a field declared as
        // dollars, this came out at 100 ARDA a unit: the first sponsor of every project was
        // charged a hundred times the founder's price and received a hundredth of the units.
        ArrangeRate(
            fundingRaised: 0,
            totalSupply: 500_000,
            backing: 10_000,
            circulating: 1_000_000,
            fundingGoal: 5_000);

        var result = await _sut.GetDeclaredRateAsync("ptc1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UsdMajorUnitsPerWholeProjectUnit.Should().Be("0.01");
        result.Value.ArdaPerWholeProjectUnit.Should().Be("1");
        result.Value.Source.Should().Be(ExchangeService.OfferingRateSource);

        // Said as the sponsor experiences it: 1,000 ARDA buys 1,000 units, not 10.
        var quoted = decimal.Parse(
            result.Value.ArdaPerWholeProjectUnit,
            System.Globalization.CultureInfo.InvariantCulture);
        (1_000m / quoted).Should().Be(1_000m);
    }

    [Fact]
    public async Task GetDeclaredRateAsync_APartlyFundedRaiseStillQuotesTheOffer_SoTheSecondSponsorPaysTheFirstsPrice()
    {
        // $50,000 asked across 500,000 units is $0.10 a unit, or 10 ARDA at $0.01 an ARDA. One
        // $10 sponsorship has settled. Handing sponsor #2 `raised / totalSupply` here would
        // quote them $0.00002 a unit - 1/5000th of what sponsor #1 paid, and enough for 1,000
        // ARDA to buy the entire supply.
        ArrangeRate(
            fundingRaised: 10,
            totalSupply: 500_000,
            backing: 10_000,
            circulating: 1_000_000,
            fundingGoal: 50_000);

        var result = await _sut.GetDeclaredRateAsync("ptc1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UsdMajorUnitsPerWholeProjectUnit.Should().Be("0.1");
        result.Value.ArdaPerWholeProjectUnit.Should().Be("10");
        result.Value.Source.Should().Be(ExchangeService.OfferingRateSource);

        // The count that must NOT move: the same 1,000 ARDA buys 100 units, never the supply.
        var quoted = decimal.Parse(
            result.Value.ArdaPerWholeProjectUnit,
            System.Globalization.CultureInfo.InvariantCulture);
        decimal.Floor(1_000m / quoted).Should().Be(100m);
    }

    [Fact]
    public async Task GetDeclaredRateAsync_AtTheMomentTheAskIsMet_TheTwoPricesAgree()
    {
        // The handover is continuous: at raised == goal both formulas are the same number, so
        // nothing jumps and nothing drops when the raise completes.
        ArrangeRate(
            fundingRaised: 50_000,
            totalSupply: 500_000,
            backing: 10_000,
            circulating: 1_000_000,
            fundingGoal: 50_000);

        var result = await _sut.GetDeclaredRateAsync("ptc1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UsdMajorUnitsPerWholeProjectUnit.Should().Be("0.1");
        result.Value.Source.Should().Be(ExchangeService.DeclaredRateSource);
    }

    /// <summary>A project whose price is whatever these three figures make it.</summary>
    private void ArrangeProject(double fundingRaised, double fundingGoal, int totalSupply)
    {
        _projectTokenConfigRepo
            .Setup(r => r.GetByIdAsync("ptc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectTokenConfig
            {
                id = "ptc1",
                assetScale = 2,
                totalSupply = totalSupply,
                fundingRaised = fundingRaised,
                fundingGoal = fundingGoal,
            });
    }

    /// <summary>A treasury holding exactly this much backing against this much circulating ARDA.</summary>
    private void ArrangeTreasury(double backing, long circulating)
    {
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

    private void ArrangeRate(
        double fundingRaised,
        int totalSupply,
        double backing,
        long circulating,
        double fundingGoal = 0)
    {
        ArrangeProject(fundingRaised, fundingGoal, totalSupply);
        ArrangeTreasury(backing, circulating);
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
