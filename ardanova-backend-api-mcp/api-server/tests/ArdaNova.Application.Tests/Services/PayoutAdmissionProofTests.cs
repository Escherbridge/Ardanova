namespace ArdaNova.Application.Tests.Services;

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

public sealed class PayoutAdmissionProofTests
{
    [Theory]
    [InlineData(false, true, -60, false)]
    [InlineData(true, false, -60, false)]
    [InlineData(true, true, 0, false)]
    [InlineData(true, true, -60, true)]
    public async Task MissingReceiptUnlocksOnlyAfterAnAttestedImmutableDeadline(bool attested, bool stamped,
        int deadlineSeconds, bool expectedUnlock)
    {
        var f = new Fixture(attested);
        f.Row.exchangeAdmitBefore = stamped ? f.Clock.Now.UtcDateTime.AddSeconds(deadlineSeconds) : null;
        await f.Service.ProcessPayoutAsync(f.Row.id);
        f.Balance.Verify(b => b.UnlockAsync("u", "c", 1, TokenHolderClass.CONTRIBUTOR, It.IsAny<CancellationToken>()),
            expectedUnlock ? Times.Once() : Times.Never());
        f.Row.status.Should().Be(expectedUnlock ? PayoutStatus.FAILED : PayoutStatus.PROCESSING);
    }

    [Fact]
    public async Task ReadThatStartsTooEarlyDoesNotBecomeProofWhileAwaitingTheAnswer()
    {
        var f = new Fixture(true);
        f.Row.exchangeAdmitBefore = f.Clock.Now.UtcDateTime;
        f.Node.Setup(n => n.GetReceiptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(() => { f.Clock.Now = f.Clock.Now.AddMinutes(10); return Task.FromResult(Result<ArdaNovaExchangeReceipt>.NotFound("absent")); });
        await f.Service.ProcessPayoutAsync(f.Row.id);
        f.Balance.Verify(b => b.UnlockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task RevokedReadPermissionDoesNotReleaseUnitsEvenAfterTheDeadline()
    {
        var f = new Fixture(true);
        f.Row.exchangeAdmitBefore = f.Clock.Now.UtcDateTime.AddMinutes(-10);
        f.Node.Setup(n => n.GetReceiptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ArdaNovaExchangeReceipt>.Forbidden("revoked"));
        await f.Service.ProcessPayoutAsync(f.Row.id);
        f.Row.status.Should().Be(PayoutStatus.PROCESSING);
        f.Balance.Verify(b => b.UnlockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task CancellationCannotRaceAnAdmissibleSend()
    {
        var f = new Fixture(true);
        f.Row.exchangeAdmitBefore = f.Clock.Now.UtcDateTime.AddMinutes(4);
        var result = await f.Service.CancelPayoutAsync(f.Row.id);
        result.Type.Should().Be(ResultType.Conflict);
        f.Balance.Verify(b => b.UnlockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task SameKeyReplaysTheNamedSaleBeforeReadingExpiredQuoteOrReadiness()
    {
        var f = new Fixture(true);
        f.Row.status = PayoutStatus.COMPLETED;
        f.Row.quoteId = Guid.NewGuid().ToString("N");
        f.Store.Setup(s => s.ReadRequestAsync("u", "key", It.IsAny<CancellationToken>())).ReturnsAsync(f.Row);
        var result = await f.Service.AcceptSaleQuoteAsync("u", new("key", f.Row.quoteId));
        result.IsSuccess.Should().BeTrue();
        f.Store.Verify(s => s.ReadQuoteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
        f.Node.Verify(n => n.ExchangeAsync(It.IsAny<Guid>(), It.IsAny<ArdaNovaExchangeCommand>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task SameKeyWithDifferentQuoteCannotAdmitAnotherSale()
    {
        var f = new Fixture(true);
        f.Row.quoteId = Guid.NewGuid().ToString("N");
        f.Store.Setup(s => s.ReadRequestAsync("u", "key", It.IsAny<CancellationToken>())).ReturnsAsync(f.Row);
        var result = await f.Service.AcceptSaleQuoteAsync("u", new("key", Guid.NewGuid().ToString("N")));
        result.Code.Should().Be("REQUEST_KEY_IN_USE");
        f.Node.Verify(n => n.ExchangeAsync(It.IsAny<Guid>(), It.IsAny<ArdaNovaExchangeCommand>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task ExpiredQuoteCanBeVoidedWithoutARecordOrNewPayment()
    {
        var f = new Fixture(true);
        var quote = new PayoutSaleQuote { id = Guid.NewGuid().ToString("N"), userId = "u", projectTokenConfigId = "c",
            sourceUnitAmount = 1, platformMinorUnits = 100, expiresAt = f.Clock.Now.UtcDateTime.AddMinutes(-1) };
        f.Store.Setup(s => s.ReadQuoteAsync("u", quote.id, It.IsAny<CancellationToken>())).ReturnsAsync(quote);
        (await f.Service.AcceptSaleQuoteAsync("u", new("key", quote.id))).Type.Should().Be(ResultType.Conflict);
        var resolution = await f.Service.VoidSaleRequestAsync("u", new("key", quote.id));
        resolution.Value!.Outcome.Should().Be("voided");
        f.Store.Verify(s => s.AddVoidAsync(It.Is<PayoutSaleVoid>(v => v.userId == "u" && v.requestKey == "key" && v.quoteId == quote.id), It.IsAny<CancellationToken>()), Times.Once());
        f.Node.Verify(n => n.ExchangeAsync(It.IsAny<Guid>(), It.IsAny<ArdaNovaExchangeCommand>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
        f.Balance.Verify(b => b.LockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task TombstonePreventsLateAdmissionBeforeAnyQuoteReadOrReadinessCheck()
    {
        var f = new Fixture(true);
        var quoteId = Guid.NewGuid().ToString("N");
        PayoutSaleVoid? tombstone = null;
        f.Store.Setup(s => s.AddVoidAsync(It.IsAny<PayoutSaleVoid>(), It.IsAny<CancellationToken>()))
            .Callback((PayoutSaleVoid row, CancellationToken _) => tombstone = row).Returns(Task.CompletedTask);
        f.Store.Setup(s => s.ReadVoidAsync("u", "key", It.IsAny<CancellationToken>())).ReturnsAsync(() => tombstone);
        (await f.Service.VoidSaleRequestAsync("u", new("key", quoteId))).Value!.Outcome.Should().Be("voided");
        var retry = await f.Service.AcceptSaleQuoteAsync("u", new("key", quoteId));
        retry.Code.Should().Be("REQUEST_VOIDED");
        f.Store.Verify(s => s.ReadQuoteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
        f.Node.Verify(n => n.ExchangeAsync(It.IsAny<Guid>(), It.IsAny<ArdaNovaExchangeCommand>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task VoidRevealsExistingAdmissionAndNeverReleasesItsUnits()
    {
        var f = new Fixture(true);
        f.Row.quoteId = Guid.NewGuid().ToString("N");
        f.Store.Setup(s => s.ReadRequestAsync("u", "key", It.IsAny<CancellationToken>())).ReturnsAsync(f.Row);
        var resolution = await f.Service.VoidSaleRequestAsync("u", new("key", f.Row.quoteId));
        resolution.Value!.Outcome.Should().Be("revealed");
        resolution.Value.Sale!.Id.Should().Be(f.Row.id);
        f.Store.Verify(s => s.AddVoidAsync(It.IsAny<PayoutSaleVoid>(), It.IsAny<CancellationToken>()), Times.Never());
        f.Balance.Verify(b => b.UnlockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Fixture
    {
        public Clock Clock { get; } = new();
        public PayoutRequest Row { get; } = new() { id = "p", userId = "u", sourceProjectTokenConfigId = "c",
            sourceTokenAmount = 1, holderClass = TokenHolderClass.CONTRIBUTOR, status = PayoutStatus.PENDING };
        public Mock<ITokenBalanceService> Balance { get; } = new();
        public Mock<IAzoaExchangeNode> Node { get; } = new();
        public Mock<IPayoutSaleStore> Store { get; } = new();
        public PayoutService Service { get; }
        public Fixture(bool attested)
        {
            var rows = new Mock<IRepository<PayoutRequest>>();
            rows.Setup(r => r.GetByIdAsync(Row.id, It.IsAny<CancellationToken>())).ReturnsAsync(Row);
            Store.Setup(s => s.LockPayoutAsync(Row.id, It.IsAny<CancellationToken>())).ReturnsAsync(Row);
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<PayoutRequestDto>(It.IsAny<PayoutRequest>()))
                .Returns((PayoutRequest row) => new PayoutRequestDto { Id = row.id, Status = row.status });
            Balance.Setup(b => b.UnlockAsync("u", "c", 1, TokenHolderClass.CONTRIBUTOR, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto()));
            Node.Setup(n => n.GetReceiptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ArdaNovaExchangeReceipt>.NotFound("absent"));
            Service = new(rows.Object, Mock.Of<IRepository<ProjectTokenConfig>>(), Mock.Of<IUnitOfWork>(), mapper.Object,
                Balance.Object, Mock.Of<IExchangeService>(), Mock.Of<IAzoaValueEligibilityService>(), Node.Object,
                timeProvider: Clock, saleStore: Store.Object, admission: new() { NodeEnforcesAdmitBefore = attested });
        }
    }
}
