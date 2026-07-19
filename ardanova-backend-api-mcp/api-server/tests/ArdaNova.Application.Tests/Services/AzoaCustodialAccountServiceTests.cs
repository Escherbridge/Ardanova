namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

public class AzoaCustodialAccountServiceTests
{
    private const string TenantId = "11111111-1111-1111-1111-111111111111";

    [Fact]
    public async Task GetCapabilitiesAsync_ProjectsExplicitDevelopmentSimulation()
    {
        var gateway = new Mock<IAzoaCustodialAccountGateway>();
        gateway.Setup(node => node.GetCapabilitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountCapabilities>.Success(new(
                Enabled: true,
                WalletChain: "Algorand",
                CustodyMode: "Development",
                CustodyAvailable: true,
                BlockchainProviderAvailable: true,
                KycProvider: "Manual",
                KycAvailable: true,
                HostedVerification: false,
                AcceptsDocumentReferences: false,
                IdentityReady: true,
                KycReady: true,
                WalletProvisioningReady: true,
                Ready: true,
                DevelopmentSimulation: true)));
        var service = CreateService(
            new Mock<IRepository<User>>(),
            new Mock<IUnitOfWork>(),
            gateway,
            TenantId);

        var result = await service.GetCapabilitiesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value!.DevelopmentSimulation.Should().BeTrue();
    }

    [Fact]
    public async Task GetCapabilitiesAsync_RejectsInconsistentAggregateReadiness()
    {
        var gateway = new Mock<IAzoaCustodialAccountGateway>();
        gateway.Setup(node => node.GetCapabilitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountCapabilities>.Success(new(
                Enabled: true,
                WalletChain: "Algorand",
                CustodyMode: "KmsHsm",
                CustodyAvailable: false,
                BlockchainProviderAvailable: true,
                KycProvider: "Hosted",
                KycAvailable: true,
                HostedVerification: true,
                AcceptsDocumentReferences: false,
                IdentityReady: true,
                KycReady: true,
                WalletProvisioningReady: false,
                Ready: true)));
        var service = CreateService(
            new Mock<IRepository<User>>(),
            new Mock<IUnitOfWork>(),
            gateway,
            TenantId);

        var result = await service.GetCapabilitiesAsync();

        result.Type.Should().Be(ResultType.Conflict);
    }

    [Fact]
    public async Task EnsureAsync_UsesStableTenantUserBindingAndPersistsOnlyThinReferences()
    {
        var users = new Mock<IRepository<User>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var gateway = new Mock<IAzoaCustodialAccountGateway>();
        var user = new User { id = "user-1", email = "user@example.test" };
        users.Setup(repository => repository.GetByIdAsync(user.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        AzoaCustodialAccountBinding? captured = null;
        gateway.Setup(node => node.EnsureAsync(It.IsAny<AzoaCustodialAccountBinding>(), It.IsAny<CancellationToken>()))
            .Callback<AzoaCustodialAccountBinding, CancellationToken>((binding, _) => captured = binding)
            .ReturnsAsync((AzoaCustodialAccountBinding binding, CancellationToken _) =>
                Result<AzoaCustodialAccountStatus>.Success(new AzoaCustodialAccountStatus(
                    binding.TenantId,
                    binding.ArdaNovaUserId,
                    "avatar-1",
                    "wallet-1",
                    "address-1",
                    AzoaKycStatus.Approved,
                    IdentityReady: true,
                    KycReady: true,
                    WalletReady: true,
                    Ready: true)));
        var service = CreateService(users, unitOfWork, gateway, TenantId);

        var first = await service.EnsureAsync(user.id);
        var firstKey = captured!.IdempotencyKey;
        var second = await service.EnsureAsync(user.id);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        captured.TenantId.Should().Be(TenantId);
        captured.ArdaNovaUserId.Should().Be(user.id);
        captured.IdempotencyKey.Should().Be(firstKey);
        captured.IdempotencyKey.Should().StartWith("ardanova-custodial-account:");
        user.azoaAvatarId.Should().Be("avatar-1");
        user.azoaWalletId.Should().Be("wallet-1");
        user.azoaWalletAddress.Should().Be("address-1");
        user.verificationLevel.Should().Be(ArdaNova.Domain.Models.Enums.VerificationLevel.PRO);
    }

    [Fact]
    public async Task EnsureAsync_RejectsNodeResponseForDifferentTenant()
    {
        var users = new Mock<IRepository<User>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var gateway = new Mock<IAzoaCustodialAccountGateway>();
        var user = new User { id = "user-1" };
        users.Setup(repository => repository.GetByIdAsync(user.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        gateway.Setup(node => node.EnsureAsync(It.IsAny<AzoaCustodialAccountBinding>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AzoaCustodialAccountBinding binding, CancellationToken _) =>
                Result<AzoaCustodialAccountStatus>.Success(new(
                    "22222222-2222-2222-2222-222222222222",
                    binding.ArdaNovaUserId,
                    "avatar-1",
                    "wallet-1",
                    "address-1",
                    AzoaKycStatus.Approved,
                    IdentityReady: true,
                    KycReady: true,
                    WalletReady: true,
                    Ready: true)));
        var service = CreateService(users, unitOfWork, gateway, TenantId);

        var result = await service.EnsureAsync(user.id);

        result.Type.Should().Be(ResultType.Conflict);
        result.Error.Should().Contain("different tenant or user");
        users.Verify(
            repository => repository.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureAsync_RejectsReadyWithoutAuthoritativeKycAndWalletEvidence()
    {
        var users = new Mock<IRepository<User>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var gateway = new Mock<IAzoaCustodialAccountGateway>();
        var user = new User { id = "user-1" };
        users.Setup(repository => repository.GetByIdAsync(user.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        gateway.Setup(node => node.EnsureAsync(It.IsAny<AzoaCustodialAccountBinding>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AzoaCustodialAccountBinding binding, CancellationToken _) =>
                Result<AzoaCustodialAccountStatus>.Success(new AzoaCustodialAccountStatus(
                    binding.TenantId,
                    binding.ArdaNovaUserId,
                    "avatar-1",
                    null,
                    null,
                    AzoaKycStatus.Pending,
                    IdentityReady: true,
                    KycReady: true,
                    WalletReady: false,
                    Ready: true)));
        var service = CreateService(users, unitOfWork, gateway, TenantId);

        var result = await service.EnsureAsync(user.id);

        result.Type.Should().Be(ResultType.Conflict);
        users.Verify(repository => repository.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureAsync_PersistsIdentityWhenWalletProvisioningIsUnavailable()
    {
        var users = new Mock<IRepository<User>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var gateway = new Mock<IAzoaCustodialAccountGateway>();
        var user = new User { id = "user-1" };
        users.Setup(repository => repository.GetByIdAsync(user.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        gateway.Setup(node => node.EnsureAsync(It.IsAny<AzoaCustodialAccountBinding>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AzoaCustodialAccountBinding binding, CancellationToken _) =>
                Result<AzoaCustodialAccountStatus>.Success(new(
                    binding.TenantId,
                    binding.ArdaNovaUserId,
                    "avatar-1",
                    null,
                    null,
                    AzoaKycStatus.Unknown,
                    IdentityReady: true,
                    KycReady: true,
                    WalletReady: false,
                    Ready: false,
                    UnavailableReason: "Wallet custody is not configured.")));
        var service = CreateService(users, unitOfWork, gateway, TenantId);

        var result = await service.EnsureAsync(user.id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IdentityReady.Should().BeTrue();
        result.Value.KycReady.Should().BeTrue();
        result.Value.WalletReady.Should().BeFalse();
        user.azoaAvatarId.Should().Be("avatar-1");
        user.azoaWalletId.Should().BeNull();
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BeginKycAsync_RejectsExpiredHostedSession()
    {
        var users = new Mock<IRepository<User>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var gateway = new Mock<IAzoaCustodialAccountGateway>();
        var user = new User { id = "user-1" };
        users.Setup(repository => repository.GetByIdAsync(user.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        gateway.Setup(node => node.BeginKycAsync(It.IsAny<AzoaCustodialAccountBinding>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaKycSession>.Success(new(
                "HostedProvider",
                HostedVerification: true,
                AcceptsDocumentReferences: false,
                VerificationUrl: "https://verify.example/session",
                ExpiresAt: DateTime.UtcNow.AddMinutes(-1),
                Instructions: null)));
        var service = CreateService(users, unitOfWork, gateway, TenantId);

        var result = await service.BeginKycAsync(user.id);

        result.Type.Should().Be(ResultType.Conflict);
        result.Error.Should().Contain("invalid hosted verification");
    }

    [Fact]
    public async Task BeginKycAsync_AllowsExplicitDevelopmentSimulationWithoutDocuments()
    {
        var users = new Mock<IRepository<User>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var gateway = new Mock<IAzoaCustodialAccountGateway>();
        var user = new User { id = "user-1" };
        users.Setup(repository => repository.GetByIdAsync(user.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        gateway.Setup(node => node.BeginKycAsync(It.IsAny<AzoaCustodialAccountBinding>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaKycSession>.Success(new(
                "Manual",
                HostedVerification: false,
                AcceptsDocumentReferences: false,
                VerificationUrl: null,
                ExpiresAt: DateTime.UtcNow.AddHours(1),
                Instructions: "Wait for the development operator review.",
                DevelopmentSimulation: true)));
        var service = CreateService(users, unitOfWork, gateway, TenantId);

        var result = await service.BeginKycAsync(user.id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DevelopmentSimulation.Should().BeTrue();
        result.Value.VerificationUrl.Should().BeNull();
    }

    [Fact]
    public async Task BeginKycAsync_RejectsUnlabelledNonHostedSession()
    {
        var users = new Mock<IRepository<User>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var gateway = new Mock<IAzoaCustodialAccountGateway>();
        var user = new User { id = "user-1" };
        users.Setup(repository => repository.GetByIdAsync(user.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        gateway.Setup(node => node.BeginKycAsync(It.IsAny<AzoaCustodialAccountBinding>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaKycSession>.Success(new(
                "PrivateReferenceProvider",
                HostedVerification: false,
                AcceptsDocumentReferences: true,
                VerificationUrl: null,
                ExpiresAt: DateTime.UtcNow.AddHours(1),
                Instructions: null,
                DevelopmentSimulation: false)));
        var service = CreateService(users, unitOfWork, gateway, TenantId);

        var result = await service.BeginKycAsync(user.id);

        result.Type.Should().Be(ResultType.Conflict);
        result.Error.Should().Contain("without an ArdaNova-supported verification method");
    }

    [Fact]
    public async Task GetStatusAsync_RejectsChangedCustodialBinding()
    {
        var users = new Mock<IRepository<User>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var gateway = new Mock<IAzoaCustodialAccountGateway>();
        var user = new User
        {
            id = "user-1",
            azoaAvatarId = "avatar-original",
            azoaWalletId = "wallet-original",
            azoaWalletAddress = "address-original",
        };
        users.Setup(repository => repository.GetByIdAsync(user.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        gateway.Setup(node => node.GetStatusAsync(It.IsAny<AzoaCustodialAccountBinding>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AzoaCustodialAccountBinding binding, CancellationToken _) =>
                Result<AzoaCustodialAccountStatus>.Success(new(
                    binding.TenantId,
                    binding.ArdaNovaUserId,
                    "avatar-different",
                    "wallet-original",
                    "address-original",
                    AzoaKycStatus.Pending,
                    IdentityReady: true,
                    KycReady: true,
                    WalletReady: true,
                    Ready: false)));
        var service = CreateService(users, unitOfWork, gateway, TenantId);

        var result = await service.GetStatusAsync(user.id);

        result.Type.Should().Be(ResultType.Conflict);
        user.azoaAvatarId.Should().Be("avatar-original");
    }

    private static AzoaCustodialAccountService CreateService(
        Mock<IRepository<User>> users,
        Mock<IUnitOfWork> unitOfWork,
        Mock<IAzoaCustodialAccountGateway> gateway,
        string tenantId)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Azoa:TenantId"] = tenantId })
            .Build();
        return new AzoaCustodialAccountService(
            users.Object,
            unitOfWork.Object,
            gateway.Object,
            configuration);
    }
}
