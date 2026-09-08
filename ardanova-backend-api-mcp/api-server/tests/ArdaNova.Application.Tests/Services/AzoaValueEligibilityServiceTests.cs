namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using FluentAssertions;
using Moq;

public class AzoaValueEligibilityServiceTests
{
    private readonly Mock<IAzoaCustodialAccountService> _custodialAccounts = new();
    private readonly AzoaValueEligibilityService _sut;

    public AzoaValueEligibilityServiceTests()
    {
        _sut = new AzoaValueEligibilityService(_custodialAccounts.Object);
    }

    [Fact]
    public async Task RequireReadyAsync_ReturnsAuthoritativeStatusWhenEveryReadinessSignalIsPresent()
    {
        var status = ReadyStatus();
        _custodialAccounts
            .Setup(service => service.GetStatusAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Success(status));

        var result = await _sut.RequireReadyAsync("user-1", "fund a project");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(status);
    }

    [Theory]
    [MemberData(nameof(IncompleteStatuses))]
    public async Task RequireReadyAsync_FailsClosedWhenAnyRequiredSignalIsMissing(
        AzoaCustodialAccountStatusDto status)
    {
        _custodialAccounts
            .Setup(service => service.GetStatusAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Success(status));

        var result = await _sut.RequireReadyAsync("user-1", "accept paid project work");

        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Contain("Complete your account setup");
        result.Error.Should().Contain("accept paid project work");
    }

    [Fact]
    public async Task RequireReadyAsync_HidesProviderFailureAndFailsClosed()
    {
        _custodialAccounts
            .Setup(service => service.GetStatusAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Failure("provider-specific diagnostic"));

        var result = await _sut.RequireReadyAsync("user-1", "receive project funding");

        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().NotContain("provider-specific diagnostic");
    }

    public static IEnumerable<object[]> IncompleteStatuses()
    {
        yield return [ReadyStatus() with { Ready = false }];
        yield return [ReadyStatus() with { IdentityReady = false }];
        yield return [ReadyStatus() with { AvatarId = null }];
        yield return [ReadyStatus() with { KycReady = false }];
        yield return [ReadyStatus() with { KycStatus = AzoaKycStatus.Pending }];
        yield return [ReadyStatus() with { WalletReady = false }];
        yield return [ReadyStatus() with { WalletId = null }];
        yield return [ReadyStatus() with { WalletAddress = null }];
    }

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
