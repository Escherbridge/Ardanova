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

public class PayoutServiceTests
{
    private readonly Mock<IRepository<PayoutRequest>> _payoutRepo;
    private readonly Mock<IRepository<ProjectTokenConfig>> _projectTokenConfigRepo;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<ITokenBalanceService> _tokenBalanceService;
    private readonly Mock<IExchangeService> _exchangeService;
    private readonly Mock<ITreasuryService> _treasuryService;
    private readonly Mock<IAzoaExchangeNode> _exchangeNode;
    private readonly Mock<IProjectUnitRateSource> _rateSource;
    private readonly Mock<IAzoaValueEligibilityService> _valueEligibility;
    private readonly PayoutService _sut;

    /// <summary>The avatar the node moves value for. Read off the readiness gate, never the body.</summary>
    private static readonly Guid AvatarId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    public PayoutServiceTests()
    {
        _payoutRepo = new Mock<IRepository<PayoutRequest>>();
        _projectTokenConfigRepo = new Mock<IRepository<ProjectTokenConfig>>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _mapper = new Mock<IMapper>();
        _tokenBalanceService = new Mock<ITokenBalanceService>();
        _exchangeService = new Mock<IExchangeService>();
        _treasuryService = new Mock<ITreasuryService>();
        _unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        // The resumable-sale read. Production's Repository.FindAsync is a ToListAsync and so
        // returns an EMPTY list, never null; an unconfigured mock returns null and would make
        // the fixture, not the service, decide the outcome. Default it to the real contract;
        // the two tests that care about existing rows override this.
        _payoutRepo
            .Setup(repo => repo.FindAsync(
                It.IsAny<Expression<Func<PayoutRequest, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PayoutRequest>());

        // Selling runs through the NODE now, and each of these three is a gate the sale must
        // pass. A fixture that omitted them would not exercise the sale path at all - it would
        // only exercise the fail-closed refusal a deployment with no exchange wired gives.
        _exchangeNode = new Mock<IAzoaExchangeNode>();
        _rateSource = new Mock<IProjectUnitRateSource>();
        _valueEligibility = new Mock<IAzoaValueEligibilityService>();

        _valueEligibility
            .Setup(gate => gate.RequireReadyAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Success(
                new AzoaCustodialAccountStatusDto { AvatarId = AvatarId.ToString(), Ready = true }));

        _rateSource
            .Setup(source => source.GetDeclaredRateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DeclaredProjectUnitRate>.Success(DeclaredRate));

        _sut = new PayoutService(
            _payoutRepo.Object,
            _projectTokenConfigRepo.Object,
            _unitOfWork.Object,
            _mapper.Object,
            _tokenBalanceService.Object,
            _exchangeService.Object,
            _valueEligibility.Object,
            _exchangeNode.Object,
            _rateSource.Object
        );
    }

    /// <summary>One whole project unit is priced at one platform unit, and at one dollar.</summary>
    private static DeclaredProjectUnitRate DeclaredRate => new(
        ProjectTokenConfigId: "config-1",
        ArdaPerWholeProjectUnit: "1",
        UsdMajorUnitsPerWholeProjectUnit: "1",
        ProjectUnitDecimals: 0,
        Source: "test",
        DeclaredAt: DateTimeOffset.UtcNow);

    /// <summary>
    /// A project whose units the node can actually move: minted, with a treasury and declared
    /// partitions. Anything less is refused by sellability before a price is ever read.
    /// </summary>
    private static ProjectTokenConfig SellableConfig(
        string configId = "config-1",
        ProjectGateStatus gateStatus = ProjectGateStatus.ACTIVE) => new()
    {
        id = configId,
        projectId = "proj-1",
        unitName = "Units",
        gateStatus = gateStatus,
        status = ProjectTokenStatus.ACTIVE,
        totalSupply = 10000,
        fundingGoal = 50000,
        fundingRaised = 60000,
        assetId = "asset-1",
        assetChain = "Algorand",
        assetScale = 0,
        treasuryAddress = "TREASURY-ADDRESS",
        treasuryReference = "treasury-1",
        partitionsDeclaredAt = DateTime.UtcNow,
    };

    /// <summary>What this person holds, as the balance service reports it.</summary>
    private void GivenBalance(
        string userId, string configId, TokenHolderClass holderClass, long balance, long locked = 0)
        => _tokenBalanceService
            .Setup(service => service.GetBalanceAsync(
                userId, configId, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto
            {
                UserId = userId,
                ProjectTokenConfigId = configId,
                HolderClass = holderClass,
                Balance = balance,
                LockedBalance = locked,
            }));

    /// <summary>A receipt the node says is finished, with nothing left to reconcile.</summary>
    private static ArdaNovaExchangeReceipt TerminalReceipt(long platformMinorUnits) => new(
        ReceiptReference: "receipt-1",
        OperationId: Guid.NewGuid(),
        State: "Settled",
        IsTerminal: true,
        RequiresReconciliation: false,
        Replayed: false,
        AmountSpentBaseUnits: null,
        AmountReceivedBaseUnits: platformMinorUnits.ToString(),
        ProjectCreditBaseUnits: null,
        SettlementReference: null,
        HoldReference: null,
        HoldReleasesAt: null,
        FailureCode: null);

    [Fact]
    public async Task RequestPayoutAsync_HappyPath_RecordsWhatTheNodeSettled()
    {
        const string userId = "user-1";
        const string configId = "config-1";
        const int sourceAmount = 1000;
        const TokenHolderClass holderClass = TokenHolderClass.CONTRIBUTOR;
        // One platform unit per project unit, at two decimals: 1000 units is 100,000 minor units.
        const long platformMinorUnits = 100_000;

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SellableConfig(configId));
        _tokenBalanceService.Setup(s => s.IsBalanceLiquidAsync(userId, configId, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        GivenBalance(userId, configId, holderClass, balance: sourceAmount);
        _tokenBalanceService.Setup(s => s.LockAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto
            {
                UserId = userId, ProjectTokenConfigId = configId, HolderClass = holderClass,
                Balance = sourceAmount, LockedBalance = sourceAmount,
            }));
        _tokenBalanceService.Setup(s => s.UnlockAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto()));
        _tokenBalanceService.Setup(s => s.DebitAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto()));
        // Snapshotted at write time. Moq re-evaluates It.Is against the CURRENT object, and the
        // settled sale mutates this very row afterwards - so asserting on it later would test
        // the end state, not the invariant that the row is PENDING before the node is called.
        PayoutStatus? statusWhenWritten = null;
        long? platformMinorUnitsWhenWritten = null;
        _payoutRepo.Setup(r => r.AddAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()))
            .Callback((PayoutRequest row, CancellationToken _) =>
            {
                statusWhenWritten = row.status;
                platformMinorUnitsWhenWritten = row.ardaMinorUnits;
            })
            .ReturnsAsync((PayoutRequest row, CancellationToken _) => row);
        _payoutRepo.Setup(r => r.UpdateAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _exchangeNode.Setup(node => node.ExchangeAsync(
                AvatarId,
                It.Is<ArdaNovaExchangeCommand>(command =>
                    !command.IsBuy && command.PlatformAmountBaseUnits == platformMinorUnits),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ArdaNovaExchangeReceipt>.Success(TerminalReceipt(platformMinorUnits)));
        _mapper.Setup(m => m.Map<PayoutRequestDto>(It.IsAny<PayoutRequest>()))
            .Returns((PayoutRequest row) => new PayoutRequestDto
            {
                Id = row.id, UserId = row.userId, Status = row.status,
                SourceTokenAmount = row.sourceTokenAmount,
            });

        var result = await _sut.RequestPayoutAsync(userId, new CreatePayoutRequestDto
        {
            SourceProjectTokenConfigId = configId,
            SourceTokenAmount = sourceAmount,
            HolderClass = holderClass
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(PayoutStatus.COMPLETED);
        result.Value.SourceTokenAmount.Should().Be(sourceAmount);

        // The row is written BEFORE the node call - the exchange key is derived from its id -
        // and carries the EXACT figures, not only the rounded display ones.
        statusWhenWritten.Should().Be(PayoutStatus.PENDING);
        platformMinorUnitsWhenWritten.Should().Be(platformMinorUnits);
        _payoutRepo.Verify(r => r.AddAsync(It.Is<PayoutRequest>(row =>
            row.userId == userId
            && row.sourceProjectTokenConfigId == configId
            && row.sourceTokenAmount == sourceAmount
            && row.usdReferenceMinorUnits == platformMinorUnits
            && row.holderClass == holderClass
            && row.gateStatusAtRequest == ProjectGateStatus.ACTIVE
        ), It.IsAny<CancellationToken>()), Times.Once);

        // Units leave exactly once, and only against a TERMINAL receipt.
        _tokenBalanceService.Verify(s => s.DebitAsync(
            userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestPayoutAsync_ReconciliationReceipt_LeavesUnitsLocked()
    {
        const string userId = "user-1";
        const string configId = "config-1";
        const int sourceAmount = 10;
        const TokenHolderClass holderClass = TokenHolderClass.CONTRIBUTOR;

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SellableConfig(configId));
        _tokenBalanceService.Setup(s => s.IsBalanceLiquidAsync(userId, configId, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        GivenBalance(userId, configId, holderClass, balance: sourceAmount);
        _tokenBalanceService.Setup(s => s.LockAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto
            {
                Balance = sourceAmount, LockedBalance = sourceAmount,
            }));
        _payoutRepo.Setup(r => r.AddAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PayoutRequest row, CancellationToken _) => row);
        _payoutRepo.Setup(r => r.UpdateAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapper.Setup(m => m.Map<PayoutRequestDto>(It.IsAny<PayoutRequest>()))
            .Returns((PayoutRequest row) => new PayoutRequestDto { Id = row.id, Status = row.status });

        var ambiguous = TerminalReceipt(1_000) with { RequiresReconciliation = true };
        _exchangeNode.Setup(node => node.ExchangeAsync(
                It.IsAny<Guid>(), It.IsAny<ArdaNovaExchangeCommand>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ArdaNovaExchangeReceipt>.Success(ambiguous));

        var result = await _sut.RequestPayoutAsync(userId, new CreatePayoutRequestDto
        {
            SourceProjectTokenConfigId = configId,
            SourceTokenAmount = sourceAmount,
            HolderClass = holderClass
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(PayoutStatus.PROCESSING);

        // Ambiguous is NOT declined: returning the units on a maybe is how one holding gets
        // sold twice, so they stay locked and nothing is debited.
        _tokenBalanceService.Verify(s => s.UnlockAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never);
        _tokenBalanceService.Verify(s => s.DebitAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestPayoutAsync_ConfigNotFound_ReturnsFailure()
    {
        const string userId = "user-1";
        const string configId = "nonexistent";

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTokenConfig?)null);

        var result = await _sut.RequestPayoutAsync(userId, new CreatePayoutRequestDto
        {
            SourceProjectTokenConfigId = configId,
            SourceTokenAmount = 1000,
            HolderClass = TokenHolderClass.CONTRIBUTOR
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
        result.Error.Should().Contain("could not be found");

        _tokenBalanceService.Verify(s => s.LockAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never);
        _exchangeNode.Verify(node => node.ExchangeAsync(
            It.IsAny<Guid>(), It.IsAny<ArdaNovaExchangeCommand>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestPayoutAsync_TokensNotLiquid_ContributorFunding_ReturnsGateMessage()
    {
        const string userId = "user-1";
        const string configId = "config-1";
        const TokenHolderClass holderClass = TokenHolderClass.CONTRIBUTOR;

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SellableConfig(configId, ProjectGateStatus.FUNDING));
        _tokenBalanceService.Setup(s => s.IsBalanceLiquidAsync(userId, configId, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        var result = await _sut.RequestPayoutAsync(userId, new CreatePayoutRequestDto
        {
            SourceProjectTokenConfigId = configId,
            SourceTokenAmount = 1000,
            HolderClass = holderClass
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        // Plain words a person can act on - no chain terms, no provider names.
        result.Error.Should().Contain("not reached its funding goal");

        _tokenBalanceService.Verify(s => s.LockAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestPayoutAsync_TokensNotLiquid_InvestorActive_ReturnsGateMessage()
    {
        const string userId = "user-1";
        const string configId = "config-1";
        const TokenHolderClass holderClass = TokenHolderClass.INVESTOR;

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SellableConfig(configId));
        _tokenBalanceService.Setup(s => s.IsBalanceLiquidAsync(userId, configId, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        var result = await _sut.RequestPayoutAsync(userId, new CreatePayoutRequestDto
        {
            SourceProjectTokenConfigId = configId,
            SourceTokenAmount = 1000,
            HolderClass = holderClass
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("has not delivered yet");

        _tokenBalanceService.Verify(s => s.LockAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestPayoutAsync_LockFails_RefusesWithoutReachingTheNode()
    {
        const string userId = "user-1";
        const string configId = "config-1";
        const int sourceAmount = 1000;
        const TokenHolderClass holderClass = TokenHolderClass.CONTRIBUTOR;

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SellableConfig(configId));
        _tokenBalanceService.Setup(s => s.IsBalanceLiquidAsync(userId, configId, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        GivenBalance(userId, configId, holderClass, balance: sourceAmount);
        _tokenBalanceService.Setup(s => s.LockAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Failure("Insufficient balance"));

        var result = await _sut.RequestPayoutAsync(userId, new CreatePayoutRequestDto
        {
            SourceProjectTokenConfigId = configId,
            SourceTokenAmount = sourceAmount,
            HolderClass = holderClass
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("nothing was moved");

        // Nothing reached the node and no row was written: units that could not be set aside
        // must not leave behind a sale to reconcile.
        _exchangeNode.Verify(node => node.ExchangeAsync(
            It.IsAny<Guid>(), It.IsAny<ArdaNovaExchangeCommand>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _payoutRepo.Verify(r => r.AddAsync(
            It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestPayoutAsync_NodeDeclines_UnlocksTokens()
    {
        const string userId = "user-1";
        const string configId = "config-1";
        const int sourceAmount = 1000;
        const TokenHolderClass holderClass = TokenHolderClass.CONTRIBUTOR;

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SellableConfig(configId));
        _tokenBalanceService.Setup(s => s.IsBalanceLiquidAsync(userId, configId, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        GivenBalance(userId, configId, holderClass, balance: sourceAmount);
        _tokenBalanceService.Setup(s => s.LockAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto
            {
                Balance = sourceAmount, LockedBalance = sourceAmount,
            }));
        _tokenBalanceService.Setup(s => s.UnlockAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto()));
        _payoutRepo.Setup(r => r.AddAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PayoutRequest row, CancellationToken _) => row);
        _payoutRepo.Setup(r => r.UpdateAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // A REFUSAL, not a timeout: the node read the request, said no, and created nothing.
        _exchangeNode.Setup(node => node.ExchangeAsync(
                It.IsAny<Guid>(), It.IsAny<ArdaNovaExchangeCommand>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ArdaNovaExchangeReceipt>.BadRequest("refused"));

        var result = await _sut.RequestPayoutAsync(userId, new CreatePayoutRequestDto
        {
            SourceProjectTokenConfigId = configId,
            SourceTokenAmount = sourceAmount,
            HolderClass = holderClass
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        // ArdaNova's own words, chosen by refusal KIND. The node's text never reaches a person.
        result.Error.Should().Contain("units are back where they were");

        _tokenBalanceService.Verify(s => s.UnlockAsync(
            userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()), Times.Once);
        _tokenBalanceService.Verify(s => s.DebitAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Reconciling is live now, so the behaviour this test guards moved: it READS the node under
    /// the key the sale was submitted with, and a terminal receipt debits the units exactly once.
    /// </summary>
    [Fact]
    public async Task ProcessPayoutAsync_TerminalReceipt_DebitsExactlyOnce()
    {
        const string userId = "user-1";
        const string configId = "config-1";
        const int sourceAmount = 1000;
        const TokenHolderClass holderClass = TokenHolderClass.CONTRIBUTOR;

        var parked = new PayoutRequest
        {
            id = "payout-1",
            userId = userId,
            sourceProjectTokenConfigId = configId,
            sourceTokenAmount = sourceAmount,
            status = PayoutStatus.PROCESSING,
            holderClass = holderClass,
            gateStatusAtRequest = ProjectGateStatus.ACTIVE,
            requestedAt = DateTime.UtcNow,
        };

        _payoutRepo.Setup(r => r.GetByIdAsync(parked.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parked);
        _payoutRepo.Setup(r => r.UpdateAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _tokenBalanceService.Setup(s => s.UnlockAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto()));
        _tokenBalanceService.Setup(s => s.DebitAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto()));
        _exchangeNode.Setup(node => node.GetReceiptAsync(
                parked.ExchangeIdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ArdaNovaExchangeReceipt>.Success(TerminalReceipt(100_000)));
        _mapper.Setup(m => m.Map<PayoutRequestDto>(It.IsAny<PayoutRequest>()))
            .Returns((PayoutRequest row) => new PayoutRequestDto { Id = row.id, Status = row.status });

        var result = await _sut.ProcessPayoutAsync(parked.id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        parked.status.Should().Be(PayoutStatus.COMPLETED);
        _tokenBalanceService.Verify(s => s.DebitAsync(
            userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()), Times.Once);
        // A read, never a second submission.
        _exchangeNode.Verify(node => node.ExchangeAsync(
            It.IsAny<Guid>(), It.IsAny<ArdaNovaExchangeCommand>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPayoutAsync_MissingRow_ReturnsNotFoundWithoutMovingValue()
    {
        _payoutRepo.Setup(r => r.GetByIdAsync("payout-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PayoutRequest?)null);

        var result = await _sut.ProcessPayoutAsync("payout-1", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
        _payoutRepo.Verify(r => r.UpdateAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _treasuryService.Verify(service => service.RebalanceIfNeededAsync(
            It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Never);
        _tokenBalanceService.Verify(service => service.DebitAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelPayoutAsync_HappyPath_CancelsAndUnlocks()
    {
        // Arrange
        var payoutId = "payout-1";
        var userId = "user-1";
        var configId = "config-1";
        var sourceAmount = 1000;
        var holderClass = TokenHolderClass.CONTRIBUTOR;

        var payout = new PayoutRequest
        {
            id = payoutId,
            userId = userId,
            sourceProjectTokenConfigId = configId,
            sourceTokenAmount = sourceAmount,
            status = PayoutStatus.PENDING,
            holderClass = holderClass,
            gateStatusAtRequest = ProjectGateStatus.ACTIVE,
            requestedAt = DateTime.UtcNow
        };

        var payoutDto = new PayoutRequestDto
        {
            Id = payoutId,
            UserId = userId,
            Status = PayoutStatus.CANCELLED
        };

        _payoutRepo.Setup(r => r.GetByIdAsync(payoutId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payout);

        _payoutRepo.Setup(r => r.UpdateAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _tokenBalanceService.Setup(s => s.UnlockAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto()));

        _mapper.Setup(m => m.Map<PayoutRequestDto>(It.IsAny<PayoutRequest>()))
            .Returns(payoutDto);

        // Act
        var result = await _sut.CancelPayoutAsync(payoutId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(PayoutStatus.CANCELLED);

        _tokenBalanceService.Verify(s => s.UnlockAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()), Times.Once);

        _payoutRepo.Verify(r => r.UpdateAsync(It.Is<PayoutRequest>(p =>
            p.status == PayoutStatus.CANCELLED
        ), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelPayoutAsync_UnlockFails_RollsBackWithoutChangingStatus()
    {
        var payout = new PayoutRequest
        {
            id = "payout-1",
            userId = "user-1",
            sourceProjectTokenConfigId = "config-1",
            sourceTokenAmount = 1000,
            status = PayoutStatus.PENDING,
            holderClass = TokenHolderClass.CONTRIBUTOR,
            gateStatusAtRequest = ProjectGateStatus.ACTIVE,
            requestedAt = DateTime.UtcNow
        };

        _payoutRepo.Setup(r => r.GetByIdAsync(payout.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payout);
        _tokenBalanceService.Setup(s => s.UnlockAsync(
                payout.userId,
                payout.sourceProjectTokenConfigId,
                payout.sourceTokenAmount,
                payout.holderClass,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Failure("Locked balance is unavailable"));

        var result = await _sut.CancelPayoutAsync(payout.id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("units could not be released");
        payout.status.Should().Be(PayoutStatus.PENDING);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(CancellationToken.None), Times.Once);
        _unitOfWork.Verify(u => u.ClearTrackedChanges(), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _payoutRepo.Verify(r => r.UpdateAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelPayoutAsync_NotPending_ReturnsFailure()
    {
        // Arrange
        var payoutId = "payout-1";

        var payout = new PayoutRequest
        {
            id = payoutId,
            userId = "user-1",
            sourceProjectTokenConfigId = "config-1",
            sourceTokenAmount = 1000,
            status = PayoutStatus.COMPLETED,
            holderClass = TokenHolderClass.CONTRIBUTOR,
            gateStatusAtRequest = ProjectGateStatus.ACTIVE,
            requestedAt = DateTime.UtcNow
        };

        _payoutRepo.Setup(r => r.GetByIdAsync(payoutId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payout);

        // Act
        var result = await _sut.CancelPayoutAsync(payoutId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("no longer waiting");

        _tokenBalanceService.Verify(s => s.UnlockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPayoutsByUserAsync_ReturnsList()
    {
        // Arrange
        var userId = "user-1";

        var payouts = new List<PayoutRequest>
        {
            new PayoutRequest
            {
                id = "payout-1",
                userId = userId,
                sourceProjectTokenConfigId = "config-1",
                sourceTokenAmount = 1000,
                status = PayoutStatus.COMPLETED,
                holderClass = TokenHolderClass.CONTRIBUTOR,
                gateStatusAtRequest = ProjectGateStatus.ACTIVE,
                requestedAt = DateTime.UtcNow
            },
            new PayoutRequest
            {
                id = "payout-2",
                userId = userId,
                sourceProjectTokenConfigId = "config-2",
                sourceTokenAmount = 500,
                status = PayoutStatus.PENDING,
                holderClass = TokenHolderClass.INVESTOR,
                gateStatusAtRequest = ProjectGateStatus.SUCCEEDED,
                requestedAt = DateTime.UtcNow
            }
        };

        var payoutDtos = new List<PayoutRequestDto>
        {
            new PayoutRequestDto { Id = "payout-1", UserId = userId, Status = PayoutStatus.COMPLETED },
            new PayoutRequestDto { Id = "payout-2", UserId = userId, Status = PayoutStatus.PENDING }
        };

        _payoutRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PayoutRequest, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payouts);

        _mapper.Setup(m => m.Map<IEnumerable<PayoutRequestDto>>(It.IsAny<IEnumerable<PayoutRequest>>()))
            .Returns(payoutDtos);

        // Act
        var result = await _sut.GetPayoutsByUserAsync(userId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(p => p.Id == "payout-1");
        result.Value.Should().Contain(p => p.Id == "payout-2");
    }

    [Fact]
    public async Task GetPendingPayoutsAsync_ReturnsList()
    {
        // Arrange
        var payouts = new List<PayoutRequest>
        {
            new PayoutRequest
            {
                id = "payout-1",
                userId = "user-1",
                sourceProjectTokenConfigId = "config-1",
                sourceTokenAmount = 1000,
                status = PayoutStatus.PENDING,
                holderClass = TokenHolderClass.CONTRIBUTOR,
                gateStatusAtRequest = ProjectGateStatus.ACTIVE,
                requestedAt = DateTime.UtcNow
            },
            new PayoutRequest
            {
                id = "payout-2",
                userId = "user-2",
                sourceProjectTokenConfigId = "config-2",
                sourceTokenAmount = 500,
                status = PayoutStatus.PENDING,
                holderClass = TokenHolderClass.INVESTOR,
                gateStatusAtRequest = ProjectGateStatus.SUCCEEDED,
                requestedAt = DateTime.UtcNow
            }
        };

        var payoutDtos = new List<PayoutRequestDto>
        {
            new PayoutRequestDto { Id = "payout-1", UserId = "user-1", Status = PayoutStatus.PENDING },
            new PayoutRequestDto { Id = "payout-2", UserId = "user-2", Status = PayoutStatus.PENDING }
        };

        _payoutRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PayoutRequest, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payouts);

        _mapper.Setup(m => m.Map<IEnumerable<PayoutRequestDto>>(It.IsAny<IEnumerable<PayoutRequest>>()))
            .Returns(payoutDtos);

        // Act
        var result = await _sut.GetPendingPayoutsAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(p => p.Status == PayoutStatus.PENDING);
    }
}
