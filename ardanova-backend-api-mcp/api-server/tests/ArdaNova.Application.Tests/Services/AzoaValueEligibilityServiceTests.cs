namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

public class AzoaValueEligibilityServiceTests
{
    private const string SetupAction = "accept paid project work";

    private readonly Mock<IAzoaCustodialAccountService> _custodialAccounts = new();
    private readonly AzoaValueEligibilityService _sut;

    public AzoaValueEligibilityServiceTests()
    {
        _sut = new AzoaValueEligibilityService(
            _custodialAccounts.Object,
            NullLogger<AzoaValueEligibilityService>.Instance);
    }

    [Fact]
    public async Task RequireReadyAsync_ReturnsAuthoritativeStatusWhenEveryReadinessSignalIsPresent()
    {
        var status = ReadyStatus();
        StubStatus(Result<AzoaCustodialAccountStatusDto>.Success(status));

        var result = await _sut.RequireReadyAsync("user-1", "fund a project");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(status);
    }

    [Theory]
    [MemberData(nameof(IncompleteStatuses))]
    public async Task RequireReadyAsync_FailsClosedWhenAnyRequiredSignalIsMissing(
        AzoaCustodialAccountStatusDto status)
    {
        StubStatus(Result<AzoaCustodialAccountStatusDto>.Success(status));

        var result = await _sut.RequireReadyAsync("user-1", SetupAction);

        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Be(ValueEligibilityMessages.SetupIncomplete(SetupAction));
    }

    [Fact]
    public async Task RequireReadyAsync_ReportsAnUnreadableAccountAsUnavailableRatherThanUnfinishedSetup()
    {
        StubStatus(Result<AzoaCustodialAccountStatusDto>.Failure("provider-specific diagnostic"));

        var result = await _sut.RequireReadyAsync("user-1", "receive project funding");

        result.Type.Should().Be(ResultType.Conflict);
        result.Error.Should().Be(ValueEligibilityMessages.ServiceUnavailable);
        result.Error.Should().NotContain("provider-specific diagnostic");
        result.Error.Should().NotContain("Complete your account setup");
    }

    [Fact]
    public async Task RequireReadyAsync_ReportsAThrownProviderFailureAsUnavailableRatherThanUnfinishedSetup()
    {
        _custodialAccounts
            .Setup(service => service.GetStatusAsync("user-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("azoa unreachable"));

        var result = await _sut.RequireReadyAsync("user-1", "receive project funding");

        result.Type.Should().Be(ResultType.Conflict);
        result.Error.Should().Be(ValueEligibilityMessages.ServiceUnavailable);
        result.Error.Should().NotContain("azoa unreachable");
        result.Error.Should().NotContain("Complete your account setup");
    }

    [Fact]
    public async Task RequireReadyAsync_ReportsASuccessfulButEmptyStatusAsUnavailable()
    {
        StubStatus(Result<AzoaCustodialAccountStatusDto>.Success(null!));

        var result = await _sut.RequireReadyAsync("user-1", SetupAction);

        result.Type.Should().Be(ResultType.Conflict);
        result.Error.Should().Be(ValueEligibilityMessages.ServiceUnavailable);
    }

    [Fact]
    public async Task RequireReadyAsync_ReportsANodeReportedOutageAsUnavailableEvenWhenSetupAlsoLooksIncomplete()
    {
        StubStatus(Result<AzoaCustodialAccountStatusDto>.Success(ReadyStatus() with
        {
            IdentityReady = false,
            KycReady = false,
            WalletReady = false,
            Ready = false,
            UnavailableReason = "AZOA tenant-bound custodial account onboarding/status is unavailable.",
        }));

        var result = await _sut.RequireReadyAsync("user-1", SetupAction);

        result.Type.Should().Be(ResultType.Conflict);
        result.Error.Should().Be(ValueEligibilityMessages.ServiceUnavailable);
        result.Error.Should().NotContain("Complete your account setup");
    }

    [Fact]
    public async Task RequireReadyAsync_FailsClosedWhenTheNodeReportsAnOutageBesideAReadyLookingAccount()
    {
        StubStatus(Result<AzoaCustodialAccountStatusDto>.Success(ReadyStatus() with
        {
            UnavailableReason = "Custody is disabled on this node.",
        }));

        var result = await _sut.RequireReadyAsync("user-1", SetupAction);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Conflict);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task RequireReadyAsync_RefusesAnUnidentifiedSubjectWithoutConsultingTheProvider(string? userId)
    {
        var result = await _sut.RequireReadyAsync(userId!, SetupAction);

        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Be(ValueEligibilityMessages.SubjectUnknown);
        _custodialAccounts.Verify(
            service => service.GetStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RequireReadyAsync_PropagatesCancellationInsteadOfReportingAnOutage()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _custodialAccounts
            .Setup(service => service.GetStatusAsync("user-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));

        var act = () => _sut.RequireReadyAsync("user-1", SetupAction, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RequireReadyAsync_TellsAHeldSubjectAReviewerOwesThemADecision()
    {
        var reviewDueAt = new DateTimeOffset(2026, 9, 21, 13, 30, 0, TimeSpan.Zero);
        StubStatus(Result<AzoaCustodialAccountStatusDto>.Success(ReadyStatus() with
        {
            KycStatus = AzoaKycStatus.UnderReview,
            ReviewDueAt = reviewDueAt,
        }));

        var result = await _sut.RequireReadyAsync("user-1", SetupAction);

        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Be(ValueEligibilityMessages.UnderReview(reviewDueAt));
        result.Error.Should().Contain("21 September 2026");

        // The two refusals this outcome exists to stop being mistaken for.
        result.Error.Should().NotContain("Complete your account setup");
        result.Error.Should().NotContain("try again shortly");
    }

    [Fact]
    public async Task RequireReadyAsync_HoldsAReviewedAccountClosedEvenWhenEverySignalLooksReady()
    {
        StubStatus(Result<AzoaCustodialAccountStatusDto>.Success(ReadyStatus() with
        {
            KycStatus = AzoaKycStatus.UnderReview,
        }));

        var result = await _sut.RequireReadyAsync("user-1", SetupAction);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Be(ValueEligibilityMessages.UnderReview(null));
        result.Error.Should().Contain("shortly");
    }

    [Fact]
    public async Task RequireReadyAsync_NeverDisclosesWhyAReviewIsOpen()
    {
        StubStatus(Result<AzoaCustodialAccountStatusDto>.Success(ReadyStatus() with
        {
            KycStatus = AzoaKycStatus.UnderReview,
            ReviewDueAt = DateTimeOffset.UtcNow.AddDays(3),
        }));

        var result = await _sut.RequireReadyAsync("user-1", SetupAction);

        foreach (var disclosure in new[] { "AML", "sanction", "screening", "watchlist", "risk", "match", "PEP" })
        {
            result.Error.Should().NotContainEquivalentOf(disclosure);
        }
    }

    [Fact]
    public async Task RequireReadyAsync_ReportsANodeOutageAheadOfAPendingReview()
    {
        StubStatus(Result<AzoaCustodialAccountStatusDto>.Success(ReadyStatus() with
        {
            KycStatus = AzoaKycStatus.UnderReview,
            ReviewDueAt = DateTimeOffset.UtcNow.AddDays(3),
            UnavailableReason = "Custody is disabled on this node.",
        }));

        var result = await _sut.RequireReadyAsync("user-1", SetupAction);

        // When the node cannot answer, nothing else it said is worth repeating to a person.
        result.Type.Should().Be(ResultType.Conflict);
        result.Error.Should().Be(ValueEligibilityMessages.ServiceUnavailable);
    }

    [Fact]
    public void UnderReview_ReadsAsAPendingDecisionRatherThanAnOutageOrUnfinishedSetup()
    {
        var withDate = ValueEligibilityMessages.UnderReview(
            new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero));

        withDate.Should().Contain("by 1 December 2026");
        withDate.Should().NotBe(ValueEligibilityMessages.ServiceUnavailable);
        withDate.Should().NotBe(ValueEligibilityMessages.SetupIncomplete(SetupAction));
        withDate.Should().NotBe(ValueEligibilityMessages.SubjectUnknown);
    }

    [Theory]
    [InlineData(ResultType.Conflict, ResultType.Conflict, 409)]
    [InlineData(ResultType.NotFound, ResultType.NotFound, 403)]
    [InlineData(ResultType.Unauthorized, ResultType.Unauthorized, 403)]
    [InlineData(ResultType.Forbidden, ResultType.Forbidden, 403)]
    public void Propagate_CarriesTheOutcomeAndItsStatusCodeToTheEdgeWithoutFlattening(
        ResultType readinessType,
        ResultType expectedType,
        int expectedStatusCode)
    {
        var readiness = ResultOfType(readinessType, "the gate's own words");

        var propagated = ValueEligibilityFailure.Propagate<AzoaCustodialAccountStatusDto, string>(readiness);

        propagated.Type.Should().Be(expectedType);
        propagated.Error.Should().Be("the gate's own words");
        ValueEligibilityFailure.StatusCode(readiness).Should().Be(expectedStatusCode);
    }

    [Fact]
    public void Propagate_KeepsAnUnconfirmableReadinessOutOfTheForbiddenOverride()
    {
        var unconfirmable = Result<AzoaCustodialAccountStatusDto>
            .Conflict(ValueEligibilityMessages.ServiceUnavailable);

        var propagated = ValueEligibilityFailure.Propagate<AzoaCustodialAccountStatusDto, string>(
            unconfirmable,
            "You cannot do that.");

        // An override may reword a Forbidden; it must never turn a 409 into a 403.
        propagated.Type.Should().Be(ResultType.Conflict);
        propagated.Error.Should().Be(ValueEligibilityMessages.ServiceUnavailable);
    }

    private static Result<AzoaCustodialAccountStatusDto> ResultOfType(ResultType type, string error) => type switch
    {
        ResultType.Conflict => Result<AzoaCustodialAccountStatusDto>.Conflict(error),
        ResultType.NotFound => Result<AzoaCustodialAccountStatusDto>.NotFound(error),
        ResultType.Unauthorized => Result<AzoaCustodialAccountStatusDto>.Unauthorized(error),
        _ => Result<AzoaCustodialAccountStatusDto>.Forbidden(error),
    };

    public static IEnumerable<object[]> IncompleteStatuses()
    {
        yield return [ReadyStatus() with { Ready = false }];
        yield return [ReadyStatus() with { IdentityReady = false }];
        yield return [ReadyStatus() with { AvatarId = null }];
        yield return [ReadyStatus() with { AvatarId = "   " }];
        yield return [ReadyStatus() with { KycReady = false }];
        yield return [ReadyStatus() with { KycStatus = AzoaKycStatus.Unknown }];
        yield return [ReadyStatus() with { KycStatus = AzoaKycStatus.Pending }];
        yield return [ReadyStatus() with { KycStatus = AzoaKycStatus.Rejected }];

        // AzoaKycStatus.UnderReview is deliberately absent: it is refused too, but with its
        // own words. Adding it here would pass only if the held case regressed to
        // "complete your account setup", which is the copy this gate exists to stop.

        yield return [ReadyStatus() with { WalletReady = false }];
        yield return [ReadyStatus() with { WalletId = null }];
        yield return [ReadyStatus() with { WalletAddress = null }];
        yield return [ReadyStatus() with { WalletAddress = "   " }];
    }

    private void StubStatus(Result<AzoaCustodialAccountStatusDto> statusResult)
        => _custodialAccounts
            .Setup(service => service.GetStatusAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(statusResult);

    private static AzoaCustodialAccountStatusDto ReadyStatus() => new()
    {
        AvatarId = "avatar-1",
        WalletId = "wallet-1",
        WalletAddress = "wallet-address-1",
        KycStatus = AzoaKycStatus.Approved,
        IdentityReady = true,
        KycReady = true,
        WalletReady = true,
        Ready = true,
    };
}
