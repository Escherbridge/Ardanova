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
    private readonly Mock<IAlgorandService> _assets = new();
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
            _assets.Object,
            new Mock<IAzoaCustodialAccountService>().Object,
            [_nodeGateway.Object]);
    }

    private readonly Mock<IAzoaProjectTokenNodeGateway> _nodeGateway = new();

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

    // -- The MAGNITUDE and PRECISION of the ask, not just its unit -----------------
    //
    // "Dollar min/max validation is enforced" was reported after the create wizard grew a
    // parseFundingGoalDollars. It was enforced on ONE screen: funding-tab.tsx parsed the same
    // field with a bare Number() and checked only `goal > 0`, the tRPC boundary took
    // z.number().positive() with no ceiling and no cent rule, and this service validated the
    // UNIT and never the figure. These pin the refusal at the door that writes the column.

    [Theory]
    // A tenth of a cent is BOTH below the floor and finer than a cent; the floor is the more
    // useful thing to be told, so it is checked first and the expectation says so.
    [InlineData(0.001, "at least")]
    [InlineData(0.5, "at least")]
    [InlineData(1_000_000_000_000d, "cannot be more than")]
    [InlineData(1.005, "finer than a cent")]
    [InlineData(5_000.005, "finer than a cent")]
    [InlineData(0d, "above zero")]
    public async Task ProvisionAsync_AnAskThatIsNotAUsableDollarFigure_IsRefusedAndNothingIsWritten(
        double fundingGoal, string expectedFragment)
    {
        ArrangeOwnedProject();

        var result = await _sut.ProvisionAsync("proj-1", Declaration(fundingGoal: fundingGoal), "owner-1");

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain(expectedFragment);
        result.Error.Should().Be(ProjectFundingLedger.DescribeUnusableGoalAmount(fundingGoal));

        // The count that must not move: no config row, no save.
        _configRepo.Verify(
            repository => repository.AddAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateConfigAsync_TheOperatorDoorAlsoRefusesAnAskOutsideTheDollarBounds()
    {
        // Same door as the unit test above, the other half of the rule: an operator could send
        // a correctly-declared USD ask of one tenth of a cent and nothing refused it.
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
            FundingGoal = 0.001,
            FundingGoalUnit = "USD",
        });

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Be(ProjectFundingLedger.DescribeUnusableGoalAmount(0.001));

        _configRepo.Verify(
            repository => repository.AddAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(1d)]                    // the floor itself
    [InlineData(1_000_000_000d)]        // the ceiling itself
    [InlineData(5_000.01)]              // an exact cent
    public async Task ProvisionAsync_AnAskAtTheExactBounds_ReachesTheColumnUnrefused(double fundingGoal)
    {
        // The other side of a bound: a rule that refuses everything is not a control either. It
        // is asserted at the SAME counter as the refusals - the config row the door writes -
        // because asserting DescribeUnusableGoalAmount(1d) is null only restates the rule to
        // itself and proves nothing about the door.
        ArrangeOwnedProject();

        ProjectTokenConfig? written = null;
        _configRepo
            .Setup(repository => repository.AddAsync(
                It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns((ProjectTokenConfig config, CancellationToken _) =>
            {
                written = config;
                return Task.FromResult(config);
            });

        // Setup stops at the stake itself: this test is about the ask reaching the column, and
        // the leg after it needs a node nothing here is standing up.
        _assets
            .Setup(assets => assets.CreateFungibleASAAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FungibleAsaCreateResult>.Failure("the stake rail is not wired in this test"));

        var result = await _sut.ProvisionAsync(
            "proj-1", Declaration(fundingGoal: fundingGoal), "owner-1");

        // The ask cleared the amount rule and was written as declared.
        written.Should().NotBeNull("an ask at the bound must reach the column the price divides");
        written!.fundingGoal.Should().Be(fundingGoal);

        // And it did not stop on the amount rule on the way.
        result.Type.Should().NotBe(ResultType.ValidationError);
        result.Error.Should().NotContain("at least")
            .And.NotContain("cannot be more than")
            .And.NotContain("finer than a cent");
    }

    [Fact]
    public async Task ProvisionAsync_ARetriedDeclarationAsksUnderTheSameKey_AndANewConfigUnderANewOne()
    {
        // The node replays a creation by its request key. A retry after a failed or timed-out
        // stake leg must present the SAME key (or a second asset strands the first), and a
        // different config row must present a different one (or it replays someone else's).
        ArrangeOwnedProject();

        ProjectTokenConfig? stored = null;
        _configRepo
            .Setup(repository => repository.AddAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns((ProjectTokenConfig config, CancellationToken _) =>
            {
                stored = config;
                return Task.FromResult(config);
            });
        _configRepo
            .Setup(repository => repository.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ProjectTokenConfig, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => stored);

        var keys = new List<string>();
        _assets
            .Setup(assets => assets.CreateFungibleASAAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, ulong, string, CancellationToken>((_, _, _, key, _) => keys.Add(key))
            .ReturnsAsync(Result<FungibleAsaCreateResult>.Failure("still being created on the node"));

        await _sut.ProvisionAsync("proj-1", Declaration(), "owner-1");
        var firstConfigId = stored!.id;
        await _sut.ProvisionAsync("proj-1", Declaration(), "owner-1");
        stored = null;
        await _sut.ProvisionAsync("proj-1", Declaration(), "owner-1");

        keys.Should().HaveCount(3);
        keys[0].Should().Contain(firstConfigId, "the key is the config row's, not the attempt's");
        keys[1].Should().Be(keys[0]);
        keys[2].Should().NotBe(keys[0]);
    }

    [Theory]
    [InlineData("Simulated", "Simulated")] // a Simulated node redirected the configured Algorand mint
    [InlineData(null, "Algorand")]         // an issuer that reports no chain keeps the configured one
    public async Task ProvisionAsync_RecordsTheChainTheStakeWasMintedOn_AndAsksTreasuryAndPartitionsThere(
        string? reportedChain, string expectedChain)
    {
        // The treasury and the allowances must live where the asset does. Asking for them on the
        // configured chain after a Simulated node minted elsewhere stopped every demo at treasury.
        ArrangeOwnedProject();

        ProjectTokenConfig? stored = null;
        _configRepo
            .Setup(repository => repository.AddAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns((ProjectTokenConfig config, CancellationToken _) =>
            {
                stored = config;
                return Task.FromResult(config);
            });
        _configRepo
            .Setup(repository => repository.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ProjectTokenConfig, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => stored);
        _assets
            .Setup(assets => assets.CreateFungibleASAAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FungibleAsaCreateResult>.Success(
                new FungibleAsaCreateResult { AssetId = "sim-asset-1", TxHash = "tx-1", ChainType = reportedChain }));

        AzoaProjectTreasuryRequest? treasuryAsked = null;
        _nodeGateway
            .Setup(gateway => gateway.ResolveTreasuryAsync(It.IsAny<AzoaProjectTreasuryRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AzoaProjectTreasuryRequest, CancellationToken>((request, _) => treasuryAsked = request)
            .ReturnsAsync((AzoaProjectTreasuryRequest request, CancellationToken _) =>
                Result<AzoaProjectTreasury>.Success(
                    new AzoaProjectTreasury(request.ChainType, request.ProjectReference, "sim:simulated:abc", "Open")));

        AzoaReservePartitionRequest? partitionsAsked = null;
        _nodeGateway
            .Setup(gateway => gateway.DeclareReservePartitionsAsync(It.IsAny<AzoaReservePartitionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AzoaReservePartitionRequest, CancellationToken>((request, _) => partitionsAsked = request)
            .ReturnsAsync((AzoaReservePartitionRequest request, CancellationToken _) =>
                Result<AzoaReservePartitions>.Success(
                    new AzoaReservePartitions(request.AssetId, request.DeclaredBaseUnitsByPartition)));

        var result = await _sut.ProvisionAsync("proj-1", Declaration(), "owner-1");

        result.IsSuccess.Should().BeTrue(result.Error);
        stored!.assetChain.Should().Be(expectedChain);
        stored.treasuryChain.Should().Be(expectedChain);
        treasuryAsked!.ChainType.Should().Be(expectedChain);
        partitionsAsked!.ChainType.Should().Be(expectedChain);
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
