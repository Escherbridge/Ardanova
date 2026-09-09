namespace ArdaNova.Application.Tests.Services;

using System.Net;
using System.Text;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Azoa;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

/// <summary>
/// OI-43. A node REFUSAL of a sell must not freeze the seller's units. The node answers a
/// refusal that provably moved nothing with HTTP 422 (its <c>REQUEST_DECLINED</c> code); the
/// transport must turn that into a DECLINED result type, and the sale must release the lock on
/// it. A 409 stays ambiguous - the exchange may already have run - and keeps the units locked.
/// </summary>
public sealed class PayoutServiceDeclinedTests
{
    private static readonly Guid AvatarId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    // ── The wire: what a 422 from the node becomes ────────────────────────────

    [Fact]
    public async Task A_422_from_the_node_is_reported_as_a_decline_not_as_an_unknown_outcome()
    {
        var client = CreateClient(HttpStatusCode.UnprocessableEntity);

        var result = await client.PostAsync<object>("/api/nft/fungible-mint", new { amount = 1 });

        // BadRequest is one of the types PayoutService treats as DECLINED, which is what lets a
        // seller's units come back. Failure or Conflict here would park them indefinitely.
        result.Type.Should().Be(ResultType.BadRequest);
        result.Type.Should().NotBe(ResultType.Failure);
        result.Type.Should().NotBe(ResultType.Conflict);
        result.Error.Should().Be("AZOA node declined the request; nothing was moved.");
    }

    [Fact]
    public async Task A_409_from_the_node_stays_ambiguous()
    {
        var client = CreateClient(HttpStatusCode.Conflict);

        var result = await client.PostAsync<object>("/api/nft/fungible-mint", new { amount = 1 });

        // The distinction this whole item exists for: "we said no" unlocks, "we do not know"
        // must not. Collapsing 409 into the declined set is how one holding gets sold twice.
        result.Type.Should().Be(ResultType.Conflict);
    }

    // ── The sale: what a decline does to the seller's units ───────────────────

    [Fact]
    public async Task A_declined_sale_releases_the_lock_and_leaves_the_row_retryable()
    {
        var fixture = new SaleFixture();
        fixture.NodeAnswers(Result<ArdaNovaExchangeReceipt>.BadRequest("declined"));

        var result = await fixture.SellAsync();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("units are back where they were");

        // The units come back exactly once, nothing is debited, and the row is FAILED rather
        // than parked in PROCESSING - so the seller can simply start another sale.
        fixture.VerifyUnlockedOnce();
        fixture.VerifyNothingDebited();
        fixture.LastWrittenStatus.Should().Be(PayoutStatus.FAILED);
        fixture.LastWrittenStatus.Should().NotBe(PayoutStatus.PROCESSING);
    }

    [Fact]
    public async Task An_ambiguous_answer_keeps_the_units_locked()
    {
        var fixture = new SaleFixture();
        fixture.NodeAnswers(Result<ArdaNovaExchangeReceipt>.Conflict("unconfirmed"));

        var result = await fixture.SellAsync();

        result.IsSuccess.Should().BeFalse();
        fixture.LastWrittenStatus.Should().Be(PayoutStatus.PROCESSING);
        fixture.VerifyNeverUnlocked();
        fixture.VerifyNothingDebited();
    }

    // ── Fixtures ──────────────────────────────────────────────────────────────

    private static AzoaNodeClient CreateClient(HttpStatusCode status)
    {
        var handler = new StubHandler(new HttpResponseMessage(status)
        {
            Content = new StringContent(
                """{"isError":true,"message":"refused","result":null}""",
                Encoding.UTF8,
                "application/json"),
        });
        return new AzoaNodeClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://azoa.example") },
            NullLogger<AzoaNodeClient>.Instance);
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    /// <summary>One sellable holding, wired so the only variable is what the node answers.</summary>
    private sealed class SaleFixture
    {
        private const string UserId = "user-1";
        private const string ConfigId = "config-1";
        private const int SourceAmount = 1000;
        private const TokenHolderClass HolderClass = TokenHolderClass.CONTRIBUTOR;

        private readonly Mock<IRepository<PayoutRequest>> _payoutRepository = new();
        private readonly Mock<IRepository<ProjectTokenConfig>> _projectTokenConfigRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ITokenBalanceService> _tokenBalanceService = new();
        private readonly Mock<IExchangeService> _exchangeService = new();
        private readonly Mock<IAzoaValueEligibilityService> _valueEligibility = new();
        private readonly Mock<IAzoaExchangeNode> _exchangeNode = new();
        private readonly Mock<IProjectUnitRateSource> _rateSource = new();
        private readonly PayoutService _sut;

        internal PayoutStatus? LastWrittenStatus { get; private set; }

        internal SaleFixture()
        {
            _unitOfWork.Setup(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _unitOfWork.Setup(unit => unit.CommitTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _unitOfWork.Setup(unit => unit.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _valueEligibility
                .Setup(gate => gate.RequireReadyAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Success(
                    new AzoaCustodialAccountStatusDto
                    {
                        AvatarId = AvatarId.ToString(),
                        Ready = true,
                    }));

            _rateSource
                .Setup(source => source.GetDeclaredRateAsync(
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<DeclaredProjectUnitRate>.Success(new DeclaredProjectUnitRate(
                    ProjectTokenConfigId: ConfigId,
                    ArdaPerWholeProjectUnit: "1",
                    UsdMajorUnitsPerWholeProjectUnit: "1",
                    ProjectUnitDecimals: 0,
                    Source: "test",
                    DeclaredAt: DateTimeOffset.UtcNow)));

            _projectTokenConfigRepository
                .Setup(repository => repository.GetByIdAsync(ConfigId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(SellableConfig());

            _tokenBalanceService
                .Setup(service => service.IsBalanceLiquidAsync(
                    UserId, ConfigId, HolderClass, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));
            _tokenBalanceService
                .Setup(service => service.GetBalanceAsync(
                    UserId, ConfigId, HolderClass, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto
                {
                    UserId = UserId,
                    ProjectTokenConfigId = ConfigId,
                    HolderClass = HolderClass,
                    Balance = SourceAmount,
                }));
            _tokenBalanceService
                .Setup(service => service.LockAsync(
                    UserId, ConfigId, SourceAmount, HolderClass, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto
                {
                    Balance = SourceAmount,
                    LockedBalance = SourceAmount,
                }));
            _tokenBalanceService
                .Setup(service => service.UnlockAsync(
                    UserId, ConfigId, SourceAmount, HolderClass, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto()));

            _payoutRepository
                .Setup(repository => repository.AddAsync(
                    It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PayoutRequest row, CancellationToken _) => row);
            // Snapshotted at write time: the row is mutated afterwards, so reading it later
            // would assert on the end state rather than on what was actually persisted.
            _payoutRepository
                .Setup(repository => repository.UpdateAsync(
                    It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()))
                .Callback((PayoutRequest row, CancellationToken _) => LastWrittenStatus = row.status)
                .Returns(Task.CompletedTask);

            _mapper.Setup(mapper => mapper.Map<PayoutRequestDto>(It.IsAny<PayoutRequest>()))
                .Returns((PayoutRequest row) => new PayoutRequestDto { Id = row.id, Status = row.status });

            _sut = new PayoutService(
                _payoutRepository.Object,
                _projectTokenConfigRepository.Object,
                _unitOfWork.Object,
                _mapper.Object,
                _tokenBalanceService.Object,
                _exchangeService.Object,
                _valueEligibility.Object,
                _exchangeNode.Object,
                _rateSource.Object);
        }

        internal void NodeAnswers(Result<ArdaNovaExchangeReceipt> answer)
            => _exchangeNode
                .Setup(node => node.ExchangeAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ArdaNovaExchangeCommand>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(answer);

        internal Task<Result<PayoutRequestDto>> SellAsync()
            => _sut.RequestPayoutAsync(
                UserId,
                new CreatePayoutRequestDto
                {
                    SourceProjectTokenConfigId = ConfigId,
                    SourceTokenAmount = SourceAmount,
                    HolderClass = HolderClass,
                },
                CancellationToken.None);

        internal void VerifyUnlockedOnce()
            => _tokenBalanceService.Verify(
                service => service.UnlockAsync(
                    UserId, ConfigId, SourceAmount, HolderClass, It.IsAny<CancellationToken>()),
                Times.Once);

        internal void VerifyNeverUnlocked()
            => _tokenBalanceService.Verify(
                service => service.UnlockAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()),
                Times.Never);

        internal void VerifyNothingDebited()
            => _tokenBalanceService.Verify(
                service => service.DebitAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()),
                Times.Never);

        private static ProjectTokenConfig SellableConfig() => new()
        {
            id = ConfigId,
            projectId = "proj-1",
            unitName = "Units",
            gateStatus = ProjectGateStatus.ACTIVE,
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
    }
}
