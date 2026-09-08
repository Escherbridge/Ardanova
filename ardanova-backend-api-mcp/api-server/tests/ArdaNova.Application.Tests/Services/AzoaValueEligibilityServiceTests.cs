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
