namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Policies;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

/// <summary>
/// The two doors that WRITE a project's ask, and the shapes of ask they must refuse.
/// </summary>
/// <remarks>
/// An ask is only meaningful against the stake it is spread over. Both refusals here exist
/// because a raise that is unreachable by construction takes real money from sponsors and then
/// never clears Gate 1, which is what makes the workers' stake liquid.
/// </remarks>
public class ProjectFundingDeclarationTests
{
    private readonly Mock<IRepository<ProjectTokenConfig>> _configRepo = new();
    private readonly Mock<IRepository<Project>> _projectRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAzoaValueEligibilityService> _valueEligibility = new();
    private readonly ProjectTokenService _sut;

    public ProjectFundingDeclarationTests()
    {
        _valueEligibility
            .Setup(gate => gate.RequireReadyAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Success(null!));

        _sut = new ProjectTokenService(
            _configRepo.Object,
            new Mock<IRepository<TokenAllocation>>().Object,
            new Mock<IRepository<TokenBalance>>().Object,
            new Mock<IRepository<PayoutRequest>>().Object,
            new Mock<IRepository<ProjectInvestment>>().Object,
            new Mock<IRepository<PlatformTreasury>>().Object,
            _unitOfWork.Object,
            new Mock<IMapper>().Object,
            new Mock<ITokenBalanceService>().Object,
            new Mock<ITreasuryService>().Object,
            new Mock<ILogger<ProjectTokenService>>().Object,
            _valueEligibility.Object,
            _projectRepo.Object,
            new Mock<IAlgorandService>().Object,
            new Mock<IAzoaCustodialAccountService>().Object,
            [new Mock<IAzoaProjectTokenNodeGateway>().Object]);
    }

    private static ProjectTokenDeclaration Declaration(
        int totalSupply = 1_000,
        int contributorSupply = 300,
        int investorSupply = 600,
        double fundingGoal = 50_000,
        string fundingGoalUnit = "USD") =>
        new()
        {
            AssetName = "Riverside Allotment Stake",
            UnitName = "PLOT",
            TotalSupply = totalSupply,
            ContributorSupply = contributorSupply,
            InvestorSupply = investorSupply,
            FundingGoal = fundingGoal,
            FundingGoalUnit = fundingGoalUnit,
            Disclosure = "A stake here is a share of what this allotment earns once the beds are built.",
        };

    private void ArrangeOwnedProject(string projectId = "proj-1", string ownerId = "owner-1") =>
        _projectRepo
            .Setup(repository => repository.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { id = projectId, createdById = ownerId });

    [Fact]
    public async Task ProvisionAsync_AsksForMoneyButSetsAsideNoSponsorStake_IsRefusedAndNothingIsWritten()
    {
        // The ask is spread across the SPONSOR units and nothing else, so an ask with no sponsor
        // units behind it is an ask nobody can answer: the offering is unpriceable, no
        // sponsorship can settle, and the gate that frees the workers' stake never clears. The
        // shape has to die at the declaration, not at the first sponsor's payment.
        ArrangeOwnedProject();

        var result = await _sut.ProvisionAsync(
            "proj-1",
            Declaration(contributorSupply: 1_000, investorSupply: 0),
            "owner-1");

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("at least one unit for sponsors");

        // The count that must not move: no config row, no save.
        _configRepo.Verify(
            repository => repository.AddAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProvisionAsync_AskDeclaredInAnythingButDollars_IsRefusedAndNothingIsWritten()
    {
        // `fundingGoal` sits beside `fundingRaised`, which accumulates dollars, and the opening
        // price divides one into the other. The refusal IS the control - no reader of two stored
        // doubles can tell which unit either of them is in.
        ArrangeOwnedProject();

        var result = await _sut.ProvisionAsync("proj-1", Declaration(fundingGoalUnit: "ARDA"), "owner-1");

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Be(ProjectFundingLedger.DescribeUnsupportedGoalUnit("ARDA"));

        _configRepo.Verify(
            repository => repository.AddAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateConfigAsync_TheOperatorDoorAlsoRefusesAnAskInAnotherUnit()
    {
        // The third writer of `fundingGoal`, and the one the earlier fix missed: an admin route
        // whose body carried a bare double with no unit on the wire at all.
        _configRepo
            .Setup(repository => repository.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ProjectTokenConfig, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTokenConfig?)null);

        var result = await _sut.CreateConfigAsync(new CreateProjectTokenConfigDto
        {
            ProjectId = "proj-1",
            AssetName = "Riverside Allotment Stake",
            UnitName = "PLOT",
            AssetScale = 6,
            TotalSupply = 1_000,
            FundingGoal = 50_000,
            FundingGoalUnit = "ARDA",
        });

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Be(ProjectFundingLedger.DescribeUnsupportedGoalUnit("ARDA"));

        _configRepo.Verify(
            repository => repository.AddAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

/// <summary>
/// The one reader the price and the gate share about what a project is actually selling.
/// </summary>
public class ProjectRaiseOfferingTests
{
    private static ProjectTokenConfig Config(
        int totalSupply = 1_000,
        int investorPartitionSupply = 600,
        int investorSupply = 0,
        double fundingGoal = 50_000,
        double fundingRaised = 0) =>
        new()
        {
            id = "ptc1",
            totalSupply = totalSupply,
            investorPartitionSupply = investorPartitionSupply,
            investorSupply = investorSupply,
            fundingGoal = fundingGoal,
            fundingRaised = fundingRaised,
        };

    [Fact]
    public void TryUnitsOnOffer_IsTheSponsorPartition_NotTheWholeSupply()
    {
        ProjectRaiseOffering.TryUnitsOnOffer(Config(), out var unitsOnOffer).Should().BeTrue();
        unitsOnOffer.Should().Be(600);
    }

    [Fact]
    public void TryUnitsOnOffer_NothingSetAsideForSponsors_HasNothingOnOffer()
    {
        ProjectRaiseOffering.TryUnitsOnOffer(Config(investorPartitionSupply: 0), out var unitsOnOffer)
            .Should().BeFalse();
        unitsOnOffer.Should().Be(0);
    }

    [Fact]
    public void SponsorStakeIsSoldOut_ADeclaredPartitionIsRequired_SoZeroIsNotSoldOut()
    {
        // `0 >= 0` reads as sold out to the arithmetic and would have cleared Gate 1 on a project
        // that never took a cent. A project that sold nothing has not sold out.
        ProjectRaiseOffering.SponsorStakeIsSoldOut(Config(investorPartitionSupply: 0)).Should().BeFalse();
        ProjectRaiseOffering.SponsorStakeIsSoldOut(Config(investorSupply: 599)).Should().BeFalse();
        ProjectRaiseOffering.SponsorStakeIsSoldOut(Config(investorSupply: 600)).Should().BeTrue();
    }

    [Fact]
    public void RaiseIsComplete_ClearsOnEitherTheAskBeingMetOrTheOfferSellingOut()
    {
        ProjectRaiseOffering.RaiseIsComplete(Config(fundingRaised: 49_999.98)).Should().BeFalse();
        ProjectRaiseOffering.RaiseIsComplete(Config(fundingRaised: 50_000)).Should().BeTrue();

        // Floored cents leave the ask a hair short even though every offered unit is gone.
        ProjectRaiseOffering.RaiseIsComplete(Config(investorSupply: 600, fundingRaised: 49_999.98))
            .Should().BeTrue();
    }

    [Fact]
    public void RaiseIsComplete_UnitsCommittedWithoutMoneyIsNotASoldOutRaise()
    {
        // `investorSupply` has a second writer: the operator's grant route. A backstop that read
        // committed UNITS alone would clear Gate 1 - and free every worker's stake - on a project
        // that had taken nothing. The money is the counter that must have moved.
        ProjectRaiseOffering.RaiseIsComplete(Config(investorSupply: 600, fundingRaised: 0))
            .Should().BeFalse();
        ProjectRaiseOffering.RaiseIsComplete(Config(investorSupply: 600, fundingRaised: 0.01))
            .Should().BeTrue();
    }

    [Fact]
    public void RaiseIsComplete_RefusesAnUndeclaredAskAndABrokenTotal()
    {
        // No ask declared: there is no raise to finish, whatever the supply columns say.
        ProjectRaiseOffering.RaiseIsComplete(Config(fundingGoal: 0, investorSupply: 600)).Should().BeFalse();

        // A below-zero total is a broken figure, not an unsponsored project; it must not clear a
        // gate on the strength of the sold-out branch alone.
        ProjectRaiseOffering.RaiseIsComplete(Config(investorSupply: 600, fundingRaised: -1)).Should().BeFalse();
    }
}
