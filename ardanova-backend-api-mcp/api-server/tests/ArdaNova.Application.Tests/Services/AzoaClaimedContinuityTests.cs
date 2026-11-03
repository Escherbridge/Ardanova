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
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

public sealed class AzoaClaimedContinuityTests
{
    [Theory]
    [InlineData("none", "[]")]
    [InlineData("granted", "[\"swap:sign\"]")]
    public async Task MinimalClaimedStatusUsesRequestBindingAndPreservesLocalMirrors(string consent, string scopes)
    {
        var handler = new Handler("{\"isError\":false,\"result\":{\"ownership\":\"claimed\",\"consent\":\"" + consent
            + "\",\"consentScopes\":" + scopes + ",\"kycStatus\":\"Unknown\",\"ready\":false}}");
        var gateway = new AzoaCustodialAccountGateway(new AzoaCustodialNodeClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://node.example") }, NullLogger<AzoaCustodialNodeClient>.Instance));
        var user = new User { id = "u", email = "u@example.test", azoaAvatarId = "stable-user", azoaWalletId = "stable-wallet",
            azoaWalletAddress = "stable-address", verificationLevel = VerificationLevel.VERIFIED };
        var users = new Mock<IRepository<User>>();
        users.Setup(u => u.GetByIdAsync("u", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var unit = new Mock<IUnitOfWork>();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Azoa:TenantId"] = "a60d1e8b-bef5-47a5-9692-195a9d83cc4d" }).Build();
        var service = new AzoaCustodialAccountService(users.Object, unit.Object, gateway, config);
        var result = await service.GetStatusAsync("u");
        result.IsSuccess.Should().BeTrue();
        result.Value!.Ownership.Should().Be("claimed");
        result.Value.Consent.Should().Be(consent);
        result.Value.Ready.Should().BeFalse();
        user.azoaWalletId.Should().Be("stable-wallet");
        user.verificationLevel.Should().Be(VerificationLevel.VERIFIED);
        users.Verify(u => u.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never());
        unit.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public void PartialOrExpiredClaimedGrantCannotDispatchAValueAction()
    {
        var now = DateTimeOffset.UtcNow;
        var status = new AzoaCustodialAccountStatusDto { Ownership = "claimed", Consent = "granted", ConsentScopes = ["kyc:read"] };
        AzoaConsentCoverage.Covers(status, "units:issue", now).Should().BeFalse();
        AzoaConsentCoverage.Covers(status with { ConsentScopes = ["units:issue"], ConsentExpiresAt = now }, "units:issue", now).Should().BeFalse();
        AzoaConsentCoverage.Covers(status with { ConsentScopes = ["units:issue"], ConsentExpiresAt = now.AddHours(1) }, "units:issue", now).Should().BeTrue();
    }

    [Theory]
    [InlineData("custodial", "swap:sign", true)]
    [InlineData("custodial", "units:issue", true)]
    [InlineData("custodial", "transfer:sign", false)]
    [InlineData("custodial", "unknown:scope", false)]
    [InlineData("unknown", "swap:sign", false)]
    [InlineData("unknown", "units:issue", false)]
    [InlineData("unknown", "transfer:sign", false)]
    public void ProvisionedParticipationDoesNotGrantCashOutOrUnknownAuthority(string ownership, string scope, bool covered)
    {
        var status = new AzoaCustodialAccountStatusDto
        {
            Ownership = ownership, Consent = "granted",
            ConsentScopes = ["swap:sign", "units:issue", "transfer:sign", "unknown:scope"],
        };
        AzoaConsentCoverage.Covers(status, scope, DateTimeOffset.UtcNow).Should().Be(covered);
    }

    [Theory]
    [InlineData("swap:sign")]
    [InlineData("units:issue")]
    [InlineData("transfer:sign")]
    public void ClaimedValueAuthorityRequiresLiveExactGrant(string scope)
    {
        var now = DateTimeOffset.UtcNow;
        var live = new AzoaCustodialAccountStatusDto
        {
            Ownership = "claimed", Consent = "granted", ConsentScopes = [scope],
            ConsentExpiresAt = now.AddMinutes(1),
        };
        AzoaConsentCoverage.Covers(live, scope, now).Should().BeTrue();
        AzoaConsentCoverage.Covers(live with { ConsentExpiresAt = now }, scope, now).Should().BeFalse();
        AzoaConsentCoverage.Covers(live with { Consent = "none" }, scope, now).Should().BeFalse();
        AzoaConsentCoverage.Covers(live with { ConsentScopes = [] }, scope, now).Should().BeFalse();
        AzoaConsentCoverage.Covers(live with { ConsentScopes = ["unknown:scope"] }, "unknown:scope", now).Should().BeFalse();
    }

    [Fact]
    public async Task HandoffUsesVerifiedServerEmailAndFixedReturnPath()
    {
        var target = Guid.NewGuid().ToString();
        var user = new User { id = "u", email = "verified@example.test", emailVerified = DateTime.UtcNow, azoaAvatarId = target };
        var users = new Mock<IRepository<User>>();
        users.Setup(u => u.GetByIdAsync("u", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var gateway = new Mock<IAzoaCustodialAccountGateway>();
        var handoffs = gateway.As<IAzoaAccountHandoffGateway>();
        handoffs.Setup(g => g.BeginHandoffAsync("u", user.email, "https://app.example/portfolio/withdraw", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AccountHandoff>.Success(new(Guid.NewGuid().ToString(), target, "custodial", "pending", DateTimeOffset.UtcNow.AddHours(1), "operator_stub")));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["Azoa:ClaimInvites:Enabled"] = "true", ["NEXTJS_URL"] = "https://app.example" }).Build();
        var service = new AzoaAccountHandoffService(users.Object, gateway.Object, config);
        var result = await service.BeginAsync("u", null);
        result.IsSuccess.Should().BeTrue();
        handoffs.VerifyAll();
        user.emailVerified = null;
        (await service.BeginAsync("u", null)).Type.Should().Be(ResultType.Forbidden);
        handoffs.Verify(g => g.BeginHandoffAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    private sealed class Handler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(body, Encoding.UTF8, "application/json") });
    }
}
