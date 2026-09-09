namespace ArdaNova.Application.Tests.Policies;

using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.Policies;
using ArdaNova.Infrastructure.Outbox;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

public class TokenLiquidityPolicyTests
{
    [Theory]
    [InlineData(TokenHolderClass.CONTRIBUTOR, ProjectGateStatus.FUNDING, false)]
    [InlineData(TokenHolderClass.CONTRIBUTOR, ProjectGateStatus.ACTIVE, true)]
    [InlineData(TokenHolderClass.CONTRIBUTOR, ProjectGateStatus.SUCCEEDED, true)]
    [InlineData(TokenHolderClass.CONTRIBUTOR, ProjectGateStatus.FAILED, false)]
    [InlineData(TokenHolderClass.INVESTOR, ProjectGateStatus.FUNDING, false)]
    [InlineData(TokenHolderClass.INVESTOR, ProjectGateStatus.ACTIVE, false)]
    [InlineData(TokenHolderClass.INVESTOR, ProjectGateStatus.SUCCEEDED, true)]
    [InlineData(TokenHolderClass.INVESTOR, ProjectGateStatus.FAILED, false)]
    [InlineData(TokenHolderClass.FOUNDER, ProjectGateStatus.FUNDING, false)]
    [InlineData(TokenHolderClass.FOUNDER, ProjectGateStatus.ACTIVE, false)]
    [InlineData(TokenHolderClass.FOUNDER, ProjectGateStatus.SUCCEEDED, true)]
    [InlineData(TokenHolderClass.FOUNDER, ProjectGateStatus.FAILED, false)]
    public void IsLiquid_UsesTheCanonicalHolderClassAndGateMatrix(
        TokenHolderClass holderClass,
        ProjectGateStatus gateStatus,
        bool expected)
    {
        TokenLiquidityPolicy.IsLiquid(holderClass, gateStatus).Should().Be(expected);
    }
}

/// <summary>The worker's own restricted window, as the one rule every spend seam reads.</summary>
public class UnitWindowTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsAwardLiquid_GateOpenButWindowStillRunning_IsNotLiquid()
    {
        var award = WorkerWindowFixtures.HeldAward(tokenAmount: 100, releasesAt: Now.AddDays(45));

        // The gate on its own would say yes. Only the window makes this false, which is the whole
        // difference OI-41 was about.
        TokenLiquidityPolicy
            .IsLiquid(TokenHolderClass.CONTRIBUTOR, ProjectGateStatus.ACTIVE)
            .Should().BeTrue();

        UnitWindow.IsAwardLiquid(award, ProjectGateStatus.ACTIVE, Now).Should().BeFalse();
    }

    [Fact]
    public void IsAwardLiquid_WindowElapsedAndGateOpen_IsLiquid()
    {
        var award = WorkerWindowFixtures.HeldAward(tokenAmount: 100, releasesAt: Now.AddDays(-1));

        UnitWindow.IsAwardLiquid(award, ProjectGateStatus.ACTIVE, Now).Should().BeTrue();
    }

    [Fact]
    public void IsAwardLiquid_WindowElapsedButGateStillFunding_IsNotLiquid()
    {
        var award = WorkerWindowFixtures.HeldAward(tokenAmount: 100, releasesAt: Now.AddDays(-1));

        UnitWindow.IsAwardLiquid(award, ProjectGateStatus.FUNDING, Now).Should().BeFalse();
    }

    [Fact]
    public void Restrict_AnAwardHeldWithNoDate_IsRestrictedRatherThanFree()
    {
        var undatable = WorkerWindowFixtures.HeldAward(tokenAmount: 70, releasesAt: null);

        var restriction = UnitWindow.Restrict(new[] { undatable }, Now);

        restriction.RestrictedUnits.Should().Be(70);
        restriction.RestrictedUntil.Should().BeNull();
    }

    [Fact]
    public void Restrict_CountsOnlyAwardsTheNodeStillHolds_AndReportsTheLatestDate()
    {
        var running = WorkerWindowFixtures.HeldAward(tokenAmount: 100, releasesAt: Now.AddDays(45));
        var later = WorkerWindowFixtures.HeldAward(tokenAmount: 25, releasesAt: Now.AddDays(60));
        var elapsed = WorkerWindowFixtures.HeldAward(tokenAmount: 40, releasesAt: Now.AddDays(-1));
        var neverHeld = WorkerWindowFixtures.FreeAward(tokenAmount: 500);

        var restriction = UnitWindow.Restrict(new[] { running, later, elapsed, neverHeld }, Now);

        restriction.RestrictedUnits.Should().Be(125);
        restriction.RestrictedUntil.Should().Be(Now.AddDays(60));
    }
}

/// <summary>
/// The seams that spend or display units, checked at the counter: the money must not move for a
/// worker whose award is still inside its window, however wide the project's gate is open.
/// </summary>
public class WorkerWindowSpendSeamTests
{
    private const string WorkerId = "worker-1";
    private const string SourceConfigId = "config-source";
    private const string TargetConfigId = "config-target";

    [Fact]
    public async Task SwapPreview_RefusesUnitsStillInsideTheWorkerWindow_EvenWithTheGateActive()
    {
        var harness = new SwapHarness(releasesAt: DateTimeOffset.UtcNow.AddDays(45));

        var result = await harness.Service.GetSwapPreviewAsync(
            WorkerId, SourceConfigId, TargetConfigId, sourceTokenAmount: 10);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("held until");

        // Nothing was quoted and nothing was written: the counter did not move.
        harness.Swaps.Verify(
            repo => repo.AddAsync(It.IsAny<ShareSwap>(), It.IsAny<CancellationToken>()), Times.Never);
        harness.UnitOfWork.Verify(
            work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SwapPreview_QuotesTheSameHoldingOnceItsWindowHasElapsed()
    {
        // The positive control for the test above: same rows, same gate, only the date moves - so
        // the refusal there can only have come from the window.
        var harness = new SwapHarness(releasesAt: DateTimeOffset.UtcNow.AddDays(-1));

        var result = await harness.Service.GetSwapPreviewAsync(
            WorkerId, SourceConfigId, TargetConfigId, sourceTokenAmount: 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SourceTokenAmount.Should().Be(10);
    }

    [Fact]
    public async Task SwapPreview_RefusesWhenTheAwardsBehindAHoldingCannotBeRead()
    {
        var harness = new SwapHarness(releasesAt: DateTimeOffset.UtcNow.AddDays(-1), withAwards: false);

        var result = await harness.Service.GetSwapPreviewAsync(
            WorkerId, SourceConfigId, TargetConfigId, sourceTokenAmount: 10);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(SwapService.NoWindowReadable);
    }

    [Fact]
    public async Task ExecuteSwap_RefusesWithTheGateThatActuallyStands()
    {
        var harness = new SwapHarness(releasesAt: DateTimeOffset.UtcNow.AddDays(-1));

        var result = await harness.Service.ExecuteSwapAsync(WorkerId, new SwapRequestDto
        {
            SourceConfigId = SourceConfigId,
            TargetConfigId = TargetConfigId,
            SourceTokenAmount = 1,
        });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(SwapService.SwapNotAvailable);

        // The old sentence blamed settlement, reconciliation and eligibility checks, all of which
        // a unit sale now runs through.
        result.Error.Should().NotContain("settlement");
        result.Error.Should().NotContain("eligibility");
    }

    [Fact]
    public async Task IsBalanceLiquid_IsFalseWhenEveryUnitIsStillInsideTheWindow()
    {
        var service = BuildBalanceService(
            heldUnits: 100, awardUnits: 100, releasesAt: DateTimeOffset.UtcNow.AddDays(45));

        var liquid = await service.IsBalanceLiquidAsync(
            WorkerId, SourceConfigId, TokenHolderClass.CONTRIBUTOR, CancellationToken.None);

        liquid.IsSuccess.Should().BeTrue();
        liquid.Value.Should().BeFalse();
    }

    [Fact]
    public async Task IsBalanceLiquid_IsTrueWhenSomeUnitsAreOutsideTheWindow()
    {
        var service = BuildBalanceService(
            heldUnits: 100, awardUnits: 30, releasesAt: DateTimeOffset.UtcNow.AddDays(45));

        var liquid = await service.IsBalanceLiquidAsync(
            WorkerId, SourceConfigId, TokenHolderClass.CONTRIBUTOR, CancellationToken.None);

        liquid.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ClearingTheFundingGate_LeavesAnAwardStillInsideItsWindowRestricted()
    {
        var configs = new Mock<IRepository<ProjectTokenConfig>>();
        var allocations = new Mock<IRepository<TokenAllocation>>();
        var balances = new Mock<IRepository<TokenBalance>>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var config = new ProjectTokenConfig
        {
            id = SourceConfigId,
            gateStatus = ProjectGateStatus.FUNDING,
            fundingGoal = 100,
            fundingRaised = 100,
        };

        var heldAward = WorkerWindowFixtures.HeldAward(
            tokenAmount: 100, releasesAt: DateTimeOffset.UtcNow.AddDays(45), recipientUserId: WorkerId);
        var freeAward = WorkerWindowFixtures.FreeAward(tokenAmount: 40, recipientUserId: "worker-2");

        var heldHolding = Holding(WorkerId, units: 100);
        var freeHolding = Holding("worker-2", units: 40);

        configs.Setup(repo => repo.GetByIdAsync(SourceConfigId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        configs.Setup(repo => repo.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        allocations.Setup(repo => repo.FindAsync(
                It.IsAny<Expression<Func<TokenAllocation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TokenAllocation> { heldAward, freeAward });
        allocations.Setup(repo => repo.UpdateAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        balances.Setup(repo => repo.FindAsync(
                It.IsAny<Expression<Func<TokenBalance, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TokenBalance> { heldHolding, freeHolding });
        balances.Setup(repo => repo.UpdateAsync(It.IsAny<TokenBalance>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new ProjectGateService(
            configs.Object,
            allocations.Object,
            balances.Object,
            unitOfWork.Object,
            new Mock<IMapper>().Object,
            new Mock<IProjectTokenService>().Object,
            new Mock<ITreasuryService>().Object);

        var result = await service.EvaluateGate1Async(SourceConfigId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        config.gateStatus.Should().Be(ProjectGateStatus.ACTIVE);

        // The gate opened for the class; it did not overwrite one worker's own window.
        heldAward.isLiquid.Should().BeFalse();
        heldHolding.isLiquid.Should().BeFalse();

        freeAward.isLiquid.Should().BeTrue();
        freeHolding.isLiquid.Should().BeTrue();

        // And the figure reported counts what actually became movable, not what was looked at.
        result.Value!.TokensUnlocked.Should().Be(80);
    }

    private static TokenBalance Holding(string userId, long units) => new()
    {
        id = $"holding-{userId}",
        userId = userId,
        projectTokenConfigId = SourceConfigId,
        holderClass = TokenHolderClass.CONTRIBUTOR,
        // Deliberately stamped liquid: the seams must read the rule, not this column.
        isLiquid = true,
        balance = units,
        lockedBalance = 0,
        updatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task AGateHeldHolding_IsNotStampedWithADateTheGateWillNotHonour()
    {
        // A worker on a still-FUNDING project. Their award carries a real release date, stamped
        // at approval, which knows nothing about the gate - and the gate refuses these units
        // whatever that date says. Publishing it would put "Available from <date>" in front of a
        // person whose release ALSO needs an outcome nobody can put a day on.
        var service = BuildBalanceService(
            heldUnits: 100,
            awardUnits: 100,
            releasesAt: DateTimeOffset.UtcNow.AddDays(45),
            gateStatus: ProjectGateStatus.FUNDING);

        var holding = await service.GetBalanceAsync(
            WorkerId, SourceConfigId, TokenHolderClass.CONTRIBUTOR, CancellationToken.None);

        holding.IsSuccess.Should().BeTrue();
        holding.Value!.IsLiquid.Should().BeFalse();
        holding.Value.RestrictedUntil.Should().BeNull(
            "the gate moves on an outcome, not on a date, so there is no date to publish");
    }

    [Fact]
    public async Task AWindowHeldHolding_CarriesTheDateItsOwnAwardsName()
    {
        // The positive control for the test above: the same rows, the same dated award, and only
        // the gate moves - so a null date there can only have come from the gate, and a rule that
        // never publishes a date would not be a control either.
        var releasesAt = DateTimeOffset.UtcNow.AddDays(45);

        var service = BuildBalanceService(
            heldUnits: 100,
            awardUnits: 100,
            releasesAt: releasesAt,
            gateStatus: ProjectGateStatus.ACTIVE);

        var holding = await service.GetBalanceAsync(
            WorkerId, SourceConfigId, TokenHolderClass.CONTRIBUTOR, CancellationToken.None);

        holding.IsSuccess.Should().BeTrue();
        holding.Value!.IsLiquid.Should().BeFalse();
        holding.Value.RestrictedUntil.Should().Be(releasesAt);
    }

    [Fact]
    public async Task AHoldWithNoDateAtAll_PublishesNoDate_EvenWithTheGateOpen()
    {
        var service = BuildBalanceService(
            heldUnits: 100,
            awardUnits: 100,
            releasesAt: null,
            gateStatus: ProjectGateStatus.ACTIVE);

        var holding = await service.GetBalanceAsync(
            WorkerId, SourceConfigId, TokenHolderClass.CONTRIBUTOR, CancellationToken.None);

        holding.Value!.IsLiquid.Should().BeFalse();
        holding.Value.RestrictedUntil.Should().BeNull(
            "a promise nobody can date is one nobody may spend");
    }

    private static TokenBalanceService BuildBalanceService(
        long heldUnits,
        int awardUnits,
        DateTimeOffset? releasesAt,
        ProjectGateStatus gateStatus = ProjectGateStatus.ACTIVE)
    {
        var balances = new Mock<IRepository<TokenBalance>>();
        var configs = new Mock<IRepository<ProjectTokenConfig>>();
        var allocations = new Mock<IRepository<TokenAllocation>>();
        var mapper = new Mock<IMapper>();

        balances.Setup(repo => repo.FindOneAsync(
                It.IsAny<Expression<Func<TokenBalance, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Holding(WorkerId, heldUnits));

        configs.Setup(repo => repo.GetByIdAsync(SourceConfigId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectTokenConfig
            {
                id = SourceConfigId,
                gateStatus = gateStatus,
            });

        allocations.Setup(repo => repo.FindAsync(
                It.IsAny<Expression<Func<TokenAllocation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TokenAllocation>
            {
                WorkerWindowFixtures.HeldAward(awardUnits, releasesAt, WorkerId),
            });

        // The DTO the read path stamps. Deliberately stamped liquid on the way in, so the flag
        // and the date on the way out can only have come from the live reading.
        mapper.Setup(map => map.Map<TokenBalanceDto>(It.IsAny<TokenBalance>()))
            .Returns((TokenBalance row) => new TokenBalanceDto
            {
                Id = row.id,
                UserId = row.userId,
                ProjectTokenConfigId = row.projectTokenConfigId,
                IsPlatformToken = row.isPlatformToken,
                HolderClass = row.holderClass,
                IsLiquid = true,
                Balance = row.balance,
                LockedBalance = row.lockedBalance,
            });

        return new TokenBalanceService(
            balances.Object,
            configs.Object,
            new Mock<IUnitOfWork>().Object,
            mapper.Object,
            allocationRepository: allocations.Object);
    }

    /// <summary>One project's units, one holder, and the awards behind them.</summary>
    private sealed class SwapHarness
    {
        public SwapHarness(DateTimeOffset releasesAt, bool withAwards = true)
        {
            var balances = new Mock<IRepository<TokenBalance>>();
            var configs = new Mock<IRepository<ProjectTokenConfig>>();
            var allocations = new Mock<IRepository<TokenAllocation>>();

            balances.Setup(repo => repo.FindOneAsync(
                    It.IsAny<Expression<Func<TokenBalance, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Holding(WorkerId, units: 100));

            configs.Setup(repo => repo.GetByIdAsync(SourceConfigId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(TradableConfig(SourceConfigId));
            configs.Setup(repo => repo.GetByIdAsync(TargetConfigId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(TradableConfig(TargetConfigId));

            allocations.Setup(repo => repo.FindAsync(
                    It.IsAny<Expression<Func<TokenAllocation, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TokenAllocation>
                {
                    WorkerWindowFixtures.HeldAward(100, releasesAt, WorkerId),
                });

            Treasury.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PlatformTreasury>
                {
                    new()
                    {
                        id = "treasury-1",
                        ardaCirculatingSupply = 100,
                        indexFundBalance = 100,
                        liquidReserveBalance = 0,
                        operationsBalance = 0,
                    },
                });

            Service = new SwapService(
                balances.Object,
                configs.Object,
                Treasury.Object,
                Swaps.Object,
                UnitOfWork.Object,
                withAwards ? allocations.Object : null);
        }

        public Mock<IRepository<ShareSwap>> Swaps { get; } = new();

        public Mock<IRepository<PlatformTreasury>> Treasury { get; } = new();

        public Mock<IUnitOfWork> UnitOfWork { get; } = new();

        public SwapService Service { get; }

        private static ProjectTokenConfig TradableConfig(string id) => new()
        {
            id = id,
            unitName = "UNIT",
            status = ProjectTokenStatus.ACTIVE,
            gateStatus = ProjectGateStatus.ACTIVE,
            totalSupply = 1000,
            fundingGoal = 1000,
            fundingRaised = 1000,
        };
    }
}

/// <summary>A parked sale settles on a timer, without waiting for somebody to notice it.</summary>
public class ParkedPayoutReconcilerTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ParkedSale_IsSettledWithoutAnOperator()
    {
        var parked = ParkedSale("sale-1", requestedAt: Now.AddMinutes(-30));
        var payouts = RepositoryOver(parked);
        var payoutService = new Mock<IPayoutService>();

        payoutService
            .Setup(service => service.ProcessPayoutAsync("sale-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PayoutRequestDto>.Success(new PayoutRequestDto
            {
                Id = "sale-1",
                Status = PayoutStatus.COMPLETED,
            }));

        var outcome = await ParkedPayoutReconcilerService.ReconcileOnceAsync(
            payouts.Object,
            payoutService.Object,
            new ParkedPayoutReconcilerOptions(),
            Now,
            NullLogger.Instance,
            CancellationToken.None);

        outcome.Examined.Should().Be(1);
        outcome.Settled.Should().Be(1);
        outcome.StillParked.Should().Be(0);

        payoutService.Verify(
            service => service.ProcessPayoutAsync("sale-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ASaleThatIsStillInFlight_IsLeftAloneUntilItIsOldEnough()
    {
        var fresh = ParkedSale("sale-2", requestedAt: Now.AddSeconds(-5));
        var payouts = RepositoryOver(fresh);
        var payoutService = new Mock<IPayoutService>(MockBehavior.Strict);

        var outcome = await ParkedPayoutReconcilerService.ReconcileOnceAsync(
            payouts.Object,
            payoutService.Object,
            new ParkedPayoutReconcilerOptions(),
            Now,
            NullLogger.Instance,
            CancellationToken.None);

        outcome.Examined.Should().Be(0);
        payoutService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ASaleTheNodeHasStillNotDecided_StaysParkedWithItsUnitsLocked()
    {
        var parked = ParkedSale("sale-3", requestedAt: Now.AddHours(-2));
        var payouts = RepositoryOver(parked);
        var payoutService = new Mock<IPayoutService>();

        payoutService
            .Setup(service => service.ProcessPayoutAsync("sale-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PayoutRequestDto>.Success(new PayoutRequestDto
            {
                Id = "sale-3",
                Status = PayoutStatus.PROCESSING,
            }));

        var outcome = await ParkedPayoutReconcilerService.ReconcileOnceAsync(
            payouts.Object,
            payoutService.Object,
            new ParkedPayoutReconcilerOptions(),
            Now,
            NullLogger.Instance,
            CancellationToken.None);

        outcome.Settled.Should().Be(0);
        outcome.StillParked.Should().Be(1);
    }

    [Fact]
    public async Task AnUnwiredDeployment_StopsTheCycleRatherThanFailingEveryRow()
    {
        var payouts = RepositoryOver(
            ParkedSale("sale-4", requestedAt: Now.AddHours(-2)),
            ParkedSale("sale-5", requestedAt: Now.AddHours(-1)));
        var payoutService = new Mock<IPayoutService>();

        payoutService
            .Setup(service => service.ProcessPayoutAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PayoutRequestDto>.Failure(PayoutService.NoExchangeConfigured));

        var outcome = await ParkedPayoutReconcilerService.ReconcileOnceAsync(
            payouts.Object,
            payoutService.Object,
            new ParkedPayoutReconcilerOptions(),
            Now,
            NullLogger.Instance,
            CancellationToken.None);

        outcome.ExchangeUnavailable.Should().BeTrue();
        payoutService.Verify(
            service => service.ProcessPayoutAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void AddParkedPayoutReconciler_RegistersTheTimedLoop()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>());

        provider.GetServices<IHostedService>()
            .OfType<ParkedPayoutReconcilerService>()
            .Should().ContainSingle();
    }

    [Fact]
    public void AddParkedPayoutReconciler_TreatsABlankSettingAsTheStatedDefault()
    {
        // The shape a deployment produces most easily: an env var that exists and is empty. A
        // binder would let it replace the C# default; these options resolve it in code.
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Payouts:Reconciler:Enabled"] = string.Empty,
            ["Payouts:Reconciler:IntervalSeconds"] = string.Empty,
            ["Payouts:Reconciler:BatchSize"] = string.Empty,
        });

        provider.GetServices<IHostedService>()
            .OfType<ParkedPayoutReconcilerService>()
            .Should().ContainSingle();

        var options = provider.GetRequiredService<ParkedPayoutReconcilerOptions>();
        options.Enabled.Should().BeTrue();
        options.Interval.Should().Be(TimeSpan.FromMinutes(2));
        options.BatchSize.Should().Be(25);
    }

    [Fact]
    public void AddParkedPayoutReconciler_HonoursAnExplicitOff()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Payouts:Reconciler:Enabled"] = "false",
        });

        provider.GetServices<IHostedService>()
            .OfType<ParkedPayoutReconcilerService>()
            .Should().BeEmpty();
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddParkedPayoutReconciler(configuration);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    /// <summary>A repository that really applies the predicate, so the cycle's filter is tested.</summary>
    private static Mock<IRepository<PayoutRequest>> RepositoryOver(params PayoutRequest[] rows)
    {
        var repository = new Mock<IRepository<PayoutRequest>>();
        repository
            .Setup(repo => repo.FindAsync(
                It.IsAny<Expression<Func<PayoutRequest, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<PayoutRequest, bool>> predicate, CancellationToken _) =>
                rows.Where(predicate.Compile()).ToList());

        return repository;
    }

    private static PayoutRequest ParkedSale(string id, DateTimeOffset requestedAt) => new()
    {
        id = id,
        userId = "worker-1",
        sourceProjectTokenConfigId = "config-source",
        sourceTokenAmount = 10,
        status = PayoutStatus.PROCESSING,
        holderClass = TokenHolderClass.CONTRIBUTOR,
        gateStatusAtRequest = ProjectGateStatus.ACTIVE,
        requestedAt = requestedAt.UtcDateTime,
    };
}

/// <summary>Awards shaped the way the approval path writes them.</summary>
internal static class WorkerWindowFixtures
{
    /// <summary>An award the node holds for its worker, released on a date (or on none at all).</summary>
    internal static TokenAllocation HeldAward(
        int tokenAmount,
        DateTimeOffset? releasesAt,
        string recipientUserId = "worker-1") => new()
    {
        id = Guid.NewGuid().ToString(),
        projectTokenConfigId = "config-source",
        recipientUserId = recipientUserId,
        tokenAmount = tokenAmount,
        status = AllocationStatus.DISTRIBUTED,
        holderClass = TokenHolderClass.CONTRIBUTOR,
        isLiquid = false,
        holdId = "hold-parent",
        workerHoldId = "hold-worker",
        workerHoldReleasesAt = releasesAt?.UtcDateTime,
        createdAt = DateTime.UtcNow,
        updatedAt = DateTime.UtcNow,
    };

    /// <summary>An award with no worker window at all - nothing holds it back but the gate.</summary>
    internal static TokenAllocation FreeAward(
        int tokenAmount,
        string recipientUserId = "worker-1") => new()
    {
        id = Guid.NewGuid().ToString(),
        projectTokenConfigId = "config-source",
        recipientUserId = recipientUserId,
        tokenAmount = tokenAmount,
        status = AllocationStatus.DISTRIBUTED,
        holderClass = TokenHolderClass.CONTRIBUTOR,
        isLiquid = false,
        createdAt = DateTime.UtcNow,
        updatedAt = DateTime.UtcNow,
    };
}
