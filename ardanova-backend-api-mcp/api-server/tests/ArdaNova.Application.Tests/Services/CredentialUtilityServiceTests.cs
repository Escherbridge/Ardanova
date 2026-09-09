namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

public class CredentialUtilityServiceTests
{
    private readonly Mock<IMembershipCredentialService> _credentialServiceMock;
    private readonly Mock<IAlgorandService> _algorandServiceMock;
    private readonly Mock<ILogger<CredentialUtilityService>> _loggerMock;
    private readonly Mock<IAzoaCredentialIssuanceNode> _issuanceNodeMock;
    private readonly Mock<IAzoaCustodialAccountService> _custodialAccountsMock;

    /// <summary>The shipped default: no issuance transport is registered, so nothing may be issued.</summary>
    private readonly CredentialUtilityService _sut;

    private const string TestPlatformAddress = "ALGO_TEST_PLATFORM_ADDRESS";

    public CredentialUtilityServiceTests()
    {
        _credentialServiceMock = new Mock<IMembershipCredentialService>();
        _algorandServiceMock = new Mock<IAlgorandService>();
        _loggerMock = new Mock<ILogger<CredentialUtilityService>>();
        _issuanceNodeMock = new Mock<IAzoaCredentialIssuanceNode>();
        _custodialAccountsMock = new Mock<IAzoaCustodialAccountService>();

        _sut = new CredentialUtilityService(
            _credentialServiceMock.Object,
            _algorandServiceMock.Object,
            CreateConfiguration(),
            _loggerMock.Object);
    }

    private static IConfiguration CreateConfiguration()
    {
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Algorand:PlatformAddress"]).Returns(TestPlatformAddress);
        return configMock.Object;
    }

    /// <summary>A service wired to a live issuance transport - the only shape allowed to record a proof.</summary>
    private CredentialUtilityService CreateServiceWithIssuanceNode()
        => new(
            _credentialServiceMock.Object,
            _algorandServiceMock.Object,
            CreateConfiguration(),
            _loggerMock.Object,
            new AlgorandProviderCapabilities("Azoa", false, false),
            _issuanceNodeMock.Object,
            _custodialAccountsMock.Object);

    /// <summary>
    /// The exact shipped default this suite exists to pin: provider <c>Simulated</c> (which reports
    /// <c>SupportsAddressBasedCredentialLifecycle == true</c>) with the issuance port null.
    /// </summary>
    private CredentialUtilityService CreateSimulatedServiceWithoutIssuanceNode()
        => new(
            _credentialServiceMock.Object,
            _algorandServiceMock.Object,
            CreateConfiguration(),
            _loggerMock.Object,
            new AlgorandProviderCapabilities("Simulated", true, true),
            issuanceNode: null,
            custodialAccounts: null);

    private void GivenProvisionedAccount(string userId, Guid avatarId)
        => _custodialAccountsMock
            .Setup(s => s.GetStatusAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Success(
                new AzoaCustodialAccountStatusDto { AvatarId = avatarId.ToString(), Ready = true }));

    private void ThenNothingWasMinted()
    {
        _algorandServiceMock.Verify(
            s => s.MintSoulboundASAAsync(
                It.IsAny<string>(),
                It.IsAny<CredentialMetadataInput>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _credentialServiceMock.Verify(
            s => s.UpdateMintInfoAsync(
                It.IsAny<string>(),
                It.IsAny<UpdateMembershipCredentialMintDto>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ========================================================================
    // GrantAndMintAsync — The shipped default fabricates nothing (OI-40)
    // ========================================================================

    /// <summary>
    /// The regression this file exists for. Provider <c>Simulated</c> reports
    /// <c>SupportsAddressBasedCredentialLifecycle == true</c>, which used to be the whole gate on
    /// the mint fallback: <c>SimulatedAlgorandService</c> invented an asset id and a transaction
    /// hash, <c>UpdateMintInfoAsync</c> persisted them, and the member's card rendered them as
    /// "Verified — independently verifiable outside ArdaNova".
    /// </summary>
    [Fact]
    public async Task GrantAndMintAsync_SimulatedProviderWithNoIssuanceTransport_RecordsNoProof()
    {
        // Arrange
        var sut = CreateSimulatedServiceWithoutIssuanceNode();
        var dto = new GrantMembershipCredentialDto
        {
            ProjectId = "project-1",
            UserId = "user-1",
            GrantedVia = "FOUNDER"
        };

        var grantedCredential = CreateCredentialDto(id: "cred-1", projectId: "project-1", userId: "user-1");
        _credentialServiceMock
            .Setup(s => s.GrantAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(grantedCredential));

        // Deliberately still armed: if the fallback ever comes back, this is what it would return.
        _algorandServiceMock
            .Setup(s => s.MintSoulboundASAAsync(It.IsAny<string>(), It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SoulboundAsaMintResult>.Success(new SoulboundAsaMintResult { AssetId = "asa-fabricated", TxHash = "tx-fabricated" }));

        // Act
        var result = await sut.GrantAndMintAsync(dto);

        // Assert — the membership is real, the proof is absent, and nothing was fabricated.
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be("cred-1");
        result.Value!.AssetId.Should().BeNull();
        result.Value!.MintTxHash.Should().BeNull();
        ThenNothingWasMinted();
    }

    [Fact]
    public async Task GrantAndMintAsync_LegacyProviderWithNoIssuanceTransport_RecordsNoProof()
    {
        // Arrange — Legacy mints a REAL asset, but to the platform's own address, so it proves
        // nothing about the member either. Same capability flag, same refusal.
        var sut = new CredentialUtilityService(
            _credentialServiceMock.Object,
            _algorandServiceMock.Object,
            CreateConfiguration(),
            _loggerMock.Object,
            new AlgorandProviderCapabilities("Legacy", false, true),
            issuanceNode: null,
            custodialAccounts: null);

        var dto = new GrantMembershipCredentialDto
        {
            GuildId = "guild-1",
            UserId = "user-1",
            GrantedVia = "CONTRIBUTION_THRESHOLD"
        };

        _credentialServiceMock
            .Setup(s => s.GrantAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(
                CreateCredentialDto(id: "cred-legacy", guildId: "guild-1", userId: "user-1")));

        // Act
        var result = await sut.GrantAndMintAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AssetId.Should().BeNull();
        ThenNothingWasMinted();
    }

    // ========================================================================
    // GrantAndMintAsync — Through the issuance node (the only proof-bearing path)
    // ========================================================================

    [Fact]
    public async Task GrantAndMintAsync_WithIssuanceNode_IssuesToTheMemberAndRecordsTheProof()
    {
        // Arrange
        var sut = CreateServiceWithIssuanceNode();
        var recipientAvatarId = Guid.NewGuid();
        var dto = new GrantMembershipCredentialDto
        {
            ProjectId = "project-1",
            UserId = "user-1",
            GrantedVia = "FOUNDER"
        };

        var grantedCredential = CreateCredentialDto(id: "cred-1", projectId: "project-1", userId: "user-1");
        _credentialServiceMock
            .Setup(s => s.GrantAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(grantedCredential));

        GivenProvisionedAccount("user-1", recipientAvatarId);

        _algorandServiceMock
            .Setup(s => s.BuildARC19MetadataAsync(It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("{\"standard\":\"arc19\"}"));

        _issuanceNodeMock
            .Setup(s => s.IssueAsync(
                recipientAvatarId,
                It.IsAny<MembershipCredentialIssueRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialIssuance>.Success(new MembershipCredentialIssuance(
                IssuanceReference: "issuance-1",
                ProofReference: "proof-1",
                MetadataUri: "ipfs://metadata-1",
                IssuedAt: DateTimeOffset.UtcNow)));

        var updatedCredential = CreateCredentialDto(
            id: "cred-1", projectId: "project-1", userId: "user-1", assetId: "proof-1", mintTxHash: "issuance-1");
        _credentialServiceMock
            .Setup(s => s.UpdateMintInfoAsync("cred-1", It.IsAny<UpdateMembershipCredentialMintDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(updatedCredential));

        // Act
        var result = await sut.GrantAndMintAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AssetId.Should().Be("proof-1");
        result.Value!.MintTxHash.Should().Be("issuance-1");

        _issuanceNodeMock.Verify(s => s.IssueAsync(
            recipientAvatarId,
            It.Is<MembershipCredentialIssueRequest>(r =>
                r.CredentialId == "cred-1" && r.Scope == "PROJECT" && r.ScopeId == "project-1"
                && r.RecipientUserId == "user-1"),
            "membership-credential-issue:cred-1",
            It.IsAny<CancellationToken>()), Times.Once);
        _credentialServiceMock.Verify(s => s.UpdateMintInfoAsync(
            "cred-1",
            It.Is<UpdateMembershipCredentialMintDto>(d => d.AssetId == "proof-1" && d.MintTxHash == "issuance-1"),
            It.IsAny<CancellationToken>()), Times.Once);
        _algorandServiceMock.Verify(s => s.MintSoulboundASAAsync(
            It.IsAny<string>(), It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ========================================================================
    // GrantAndMintAsync — Issuance refused (membership stands, proof does not)
    // ========================================================================

    [Fact]
    public async Task GrantAndMintAsync_IssuanceRefused_KeepsTheCredentialWithoutAProof()
    {
        // Arrange
        var sut = CreateServiceWithIssuanceNode();
        var recipientAvatarId = Guid.NewGuid();
        var dto = new GrantMembershipCredentialDto
        {
            GuildId = "guild-1",
            UserId = "user-1",
            GrantedVia = "CONTRIBUTION_THRESHOLD"
        };

        var grantedCredential = CreateCredentialDto(id: "cred-2", guildId: "guild-1", userId: "user-1");
        _credentialServiceMock
            .Setup(s => s.GrantAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(grantedCredential));

        GivenProvisionedAccount("user-1", recipientAvatarId);

        _algorandServiceMock
            .Setup(s => s.BuildARC19MetadataAsync(It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("{\"standard\":\"arc19\"}"));

        _issuanceNodeMock
            .Setup(s => s.IssueAsync(
                It.IsAny<Guid>(),
                It.IsAny<MembershipCredentialIssueRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialIssuance>.Failure("The issuance node is unreachable"));

        // Act
        var result = await sut.GrantAndMintAsync(dto);

        // Assert - the membership stands; what the member does not get is a proof they lack.
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be("cred-2");
        result.Value!.AssetId.Should().BeNull("the issuance node refused, so there is no proof");
        ThenNothingWasMinted();
    }

    // ========================================================================
    // GrantAndMintAsync — Grant Fails
    // ========================================================================

    [Fact]
    public async Task GrantAndMintAsync_GrantFails_ReturnsFailureWithoutMinting()
    {
        // Arrange
        var dto = new GrantMembershipCredentialDto
        {
            ProjectId = "project-1",
            UserId = "user-1",
            GrantedVia = "FOUNDER"
        };

        _credentialServiceMock
            .Setup(s => s.GrantAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.ValidationError("User already has an active credential"));

        // Act
        var result = await _sut.GrantAndMintAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("already has an active credential");

        _algorandServiceMock.Verify(s => s.MintSoulboundASAAsync(
            It.IsAny<string>(), It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ========================================================================
    // RevokeAndBurnAsync — Happy Path
    // ========================================================================

    [Fact]
    public async Task RevokeAndBurnAsync_HappyPath_BurnsASAAndRevokesCredential()
    {
        // Arrange
        var credentialId = "cred-1";
        var credential = CreateCredentialDto(id: credentialId, assetId: "asa-123", status: "ACTIVE");

        _credentialServiceMock
            .Setup(s => s.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(credential));

        _algorandServiceMock
            .Setup(s => s.BurnASAAsync("asa-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("tx-burn-xyz"));

        var revokedCredential = CreateCredentialDto(id: credentialId, status: "REVOKED", revokeTxHash: "tx-burn-xyz");
        _credentialServiceMock
            .Setup(s => s.RevokeAsync(credentialId, It.IsAny<RevokeMembershipCredentialDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(revokedCredential));

        // Act
        var result = await _sut.RevokeAndBurnAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("REVOKED");
        result.Value!.RevokeTxHash.Should().Be("tx-burn-xyz");

        _algorandServiceMock.Verify(s => s.BurnASAAsync("asa-123", It.IsAny<CancellationToken>()), Times.Once);
        _credentialServiceMock.Verify(s => s.RevokeAsync(
            credentialId,
            It.Is<RevokeMembershipCredentialDto>(d => d.RevokeTxHash == "tx-burn-xyz"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // RevokeAndBurnAsync — No AssetId (off-chain only)
    // ========================================================================

    [Fact]
    public async Task RevokeAndBurnAsync_NoAssetId_RevokesWithoutBurning()
    {
        // Arrange
        var credentialId = "cred-2";
        var credential = CreateCredentialDto(id: credentialId, assetId: null, status: "ACTIVE");

        _credentialServiceMock
            .Setup(s => s.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(credential));

        var revokedCredential = CreateCredentialDto(id: credentialId, status: "REVOKED");
        _credentialServiceMock
            .Setup(s => s.RevokeAsync(credentialId, It.IsAny<RevokeMembershipCredentialDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(revokedCredential));

        // Act
        var result = await _sut.RevokeAndBurnAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("REVOKED");

        _algorandServiceMock.Verify(s => s.BurnASAAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ========================================================================
    // RevokeAndBurnAsync — Credential Not Found
    // ========================================================================

    [Fact]
    public async Task RevokeAndBurnAsync_CredentialNotFound_ReturnsNotFound()
    {
        // Arrange
        _credentialServiceMock
            .Setup(s => s.GetByIdAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.NotFound("Not found"));

        // Act
        var result = await _sut.RevokeAndBurnAsync("nonexistent");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // ========================================================================
    // UpgradeTierAsync — Happy Path
    // ========================================================================

    [Fact]
    public async Task UpgradeTierAsync_ValidUpgrade_UpdatesTier()
    {
        // Arrange
        var credentialId = "cred-1";
        var credential = CreateCredentialDto(id: credentialId, tier: "BRONZE", status: "ACTIVE");

        _credentialServiceMock
            .Setup(s => s.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(credential));

        var updatedCredential = CreateCredentialDto(id: credentialId, tier: "SILVER", status: "ACTIVE");
        _credentialServiceMock
            .Setup(s => s.UpdateTierAsync(credentialId, It.Is<UpdateCredentialTierDto>(d => d.Tier == "SILVER"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(updatedCredential));

        // Act
        var result = await _sut.UpgradeTierAsync(credentialId, "SILVER");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be("SILVER");
    }

    // ========================================================================
    // UpgradeTierAsync — Downgrade Rejected
    // ========================================================================

    [Fact]
    public async Task UpgradeTierAsync_Downgrade_ReturnsValidationError()
    {
        // Arrange
        var credentialId = "cred-1";
        var credential = CreateCredentialDto(id: credentialId, tier: "GOLD", status: "ACTIVE");

        _credentialServiceMock
            .Setup(s => s.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(credential));

        // Act
        var result = await _sut.UpgradeTierAsync(credentialId, "BRONZE");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("can only go up");

        _credentialServiceMock.Verify(s => s.UpdateTierAsync(
            It.IsAny<string>(), It.IsAny<UpdateCredentialTierDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ========================================================================
    // UpgradeTierAsync — Same Tier Rejected
    // ========================================================================

    [Fact]
    public async Task UpgradeTierAsync_SameTier_ReturnsValidationError()
    {
        // Arrange
        var credentialId = "cred-1";
        var credential = CreateCredentialDto(id: credentialId, tier: "SILVER", status: "ACTIVE");

        _credentialServiceMock
            .Setup(s => s.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(credential));

        // Act
        var result = await _sut.UpgradeTierAsync(credentialId, "SILVER");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    // ========================================================================
    // UpgradeTierAsync — Invalid Tier
    // ========================================================================

    [Fact]
    public async Task UpgradeTierAsync_InvalidTier_ReturnsValidationError()
    {
        // Arrange
        var credentialId = "cred-1";
        var credential = CreateCredentialDto(id: credentialId, tier: "BRONZE", status: "ACTIVE");

        _credentialServiceMock
            .Setup(s => s.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(credential));

        // Act
        var result = await _sut.UpgradeTierAsync(credentialId, "MYTHIC");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("Invalid tier");
    }

    // ========================================================================
    // UpgradeTierAsync — Null Current Tier (first assignment)
    // ========================================================================

    [Fact]
    public async Task UpgradeTierAsync_NullCurrentTier_AllowsAnyValidTier()
    {
        // Arrange
        var credentialId = "cred-1";
        var credential = CreateCredentialDto(id: credentialId, tier: null, status: "ACTIVE");

        _credentialServiceMock
            .Setup(s => s.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(credential));

        var updatedCredential = CreateCredentialDto(id: credentialId, tier: "GOLD", status: "ACTIVE");
        _credentialServiceMock
            .Setup(s => s.UpdateTierAsync(credentialId, It.Is<UpdateCredentialTierDto>(d => d.Tier == "GOLD"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(updatedCredential));

        // Act
        var result = await _sut.UpgradeTierAsync(credentialId, "GOLD");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be("GOLD");
    }

    // ========================================================================
    // CheckAndAutoGrantAsync — Already Has Credential
    // ========================================================================

    [Fact]
    public async Task CheckAndAutoGrantAsync_AlreadyHasCredential_ReturnsNull()
    {
        // Arrange
        _credentialServiceMock
            .Setup(s => s.CheckEligibilityAsync("user-1", "project-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CredentialEligibilityDto>.Success(new CredentialEligibilityDto
            {
                IsEligible = false,
                Reason = "User already has an active credential for this scope"
            }));

        // Act
        var result = await _sut.CheckAndAutoGrantAsync("user-1", "project-1", null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    // ========================================================================
    // CheckAndAutoGrantAsync — Eligible, grants without fabricating a proof
    // ========================================================================

    [Fact]
    public async Task CheckAndAutoGrantAsync_Eligible_GrantsWithoutFabricatingAProof()
    {
        // Arrange
        _credentialServiceMock
            .Setup(s => s.CheckEligibilityAsync("user-1", "project-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CredentialEligibilityDto>.Success(new CredentialEligibilityDto
            {
                IsEligible = true
            }));

        var grantedCredential = CreateCredentialDto(id: "cred-auto", projectId: "project-1", userId: "user-1");
        _credentialServiceMock
            .Setup(s => s.GrantAsync(It.IsAny<GrantMembershipCredentialDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(grantedCredential));

        // Act
        var result = await _sut.CheckAndAutoGrantAsync("user-1", "project-1", null);

        // Assert — auto-grant routes through GrantAndMintAsync, so it inherits the same refusal.
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be("cred-auto");
        result.Value!.AssetId.Should().BeNull();
        ThenNothingWasMinted();
    }

    // ========================================================================
    // RetryMintAsync — No transport means nothing to retry (OI-40)
    // ========================================================================

    [Fact]
    public async Task RetryMintAsync_WithoutIssuanceTransport_RefusesInsteadOfMinting()
    {
        // Arrange — the shipped default. A "retry" here used to run the same fabricating mint.
        var sut = CreateSimulatedServiceWithoutIssuanceNode();
        const string credentialId = "cred-retry";

        _credentialServiceMock
            .Setup(s => s.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(
                CreateCredentialDto(id: credentialId, projectId: "project-1", userId: "user-1", status: "ACTIVE")));

        _algorandServiceMock
            .Setup(s => s.MintSoulboundASAAsync(It.IsAny<string>(), It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SoulboundAsaMintResult>.Success(new SoulboundAsaMintResult { AssetId = "asa-fabricated", TxHash = "tx-fabricated" }));

        // Act
        var result = await sut.RetryMintAsync(credentialId);

        // Assert — a stated refusal that names the missing transport, not a manufactured proof.
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Conflict);
        result.Error.Should().Be(CredentialUtilityService.IssuanceTransportUnavailable);
        ThenNothingWasMinted();
    }

    // ========================================================================
    // RetryMintAsync — Happy Path (through the issuance node)
    // ========================================================================

    [Fact]
    public async Task RetryMintAsync_WithIssuanceNode_IssuesAndRecordsTheProof()
    {
        // Arrange
        var sut = CreateServiceWithIssuanceNode();
        var recipientAvatarId = Guid.NewGuid();
        const string credentialId = "cred-retry";
        var credential = CreateCredentialDto(id: credentialId, projectId: "project-1", userId: "user-1", assetId: null, status: "ACTIVE");

        _credentialServiceMock
            .Setup(s => s.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(credential));

        GivenProvisionedAccount("user-1", recipientAvatarId);

        _algorandServiceMock
            .Setup(s => s.BuildARC19MetadataAsync(It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("{\"standard\":\"arc19\"}"));

        _issuanceNodeMock
            .Setup(s => s.IssueAsync(
                recipientAvatarId,
                It.IsAny<MembershipCredentialIssueRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialIssuance>.Success(new MembershipCredentialIssuance(
                IssuanceReference: "issuance-retry",
                ProofReference: "proof-retry",
                MetadataUri: null,
                IssuedAt: DateTimeOffset.UtcNow)));

        var updatedCredential = CreateCredentialDto(
            id: credentialId, projectId: "project-1", assetId: "proof-retry", mintTxHash: "issuance-retry");
        _credentialServiceMock
            .Setup(s => s.UpdateMintInfoAsync(credentialId, It.IsAny<UpdateMembershipCredentialMintDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(updatedCredential));

        // Act
        var result = await sut.RetryMintAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AssetId.Should().Be("proof-retry");
        result.Value!.MintTxHash.Should().Be("issuance-retry");
        _algorandServiceMock.Verify(s => s.MintSoulboundASAAsync(
            It.IsAny<string>(), It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ========================================================================
    // RetryMintAsync — Already Verified
    // ========================================================================

    [Fact]
    public async Task RetryMintAsync_AlreadyVerified_ReturnsValidationError()
    {
        // Arrange
        var sut = CreateServiceWithIssuanceNode();
        const string credentialId = "cred-already";
        var credential = CreateCredentialDto(id: credentialId, assetId: "proof-exists", status: "ACTIVE");

        _credentialServiceMock
            .Setup(s => s.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(credential));

        // Act
        var result = await sut.RetryMintAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("already verified");
    }

    // ========================================================================
    // RetryMintAsync — Not Active
    // ========================================================================

    [Fact]
    public async Task RetryMintAsync_NotActive_ReturnsValidationError()
    {
        // Arrange
        var sut = CreateServiceWithIssuanceNode();
        const string credentialId = "cred-revoked";
        var credential = CreateCredentialDto(id: credentialId, assetId: null, status: "REVOKED");

        _credentialServiceMock
            .Setup(s => s.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(credential));

        // Act
        var result = await sut.RetryMintAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("active");
    }

    // ========================================================================
    // GetCredentialWithChainDataAsync — With On-Chain Data
    // ========================================================================

    [Fact]
    public async Task GetCredentialWithChainDataAsync_WithAssetId_ReturnsEnrichedData()
    {
        // Arrange
        var credentialId = "cred-chain";
        var credential = CreateCredentialDto(id: credentialId, assetId: "asa-456", status: "ACTIVE");

        _credentialServiceMock
            .Setup(s => s.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(credential));

        _algorandServiceMock
            .Setup(s => s.GetASAInfoAsync("asa-456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AsaInfoDto>.Success(new AsaInfoDto
            {
                AssetId = "asa-456",
                AssetName = "Test Credential",
                UnitName = "CRED",
                Total = 1,
                DefaultFrozen = true
            }));

        _algorandServiceMock
            .Setup(s => s.VerifyOwnershipAsync("asa-456", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        var result = await _sut.GetCredentialWithChainDataAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Credential.Id.Should().Be(credentialId);
        result.Value!.IsOnChain.Should().BeTrue();
        result.Value!.AsaInfo.Should().NotBeNull();
        result.Value!.AsaInfo!.AssetId.Should().Be("asa-456");
        result.Value!.ChainVerified.Should().BeTrue();
    }

    // ========================================================================
    // GetCredentialWithChainDataAsync — Without On-Chain Data
    // ========================================================================

    [Fact]
    public async Task GetCredentialWithChainDataAsync_WithoutAssetId_ReturnsOffChainOnly()
    {
        // Arrange
        var credentialId = "cred-offchain";
        var credential = CreateCredentialDto(id: credentialId, assetId: null, status: "ACTIVE");

        _credentialServiceMock
            .Setup(s => s.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(credential));

        // Act
        var result = await _sut.GetCredentialWithChainDataAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Credential.Id.Should().Be(credentialId);
        result.Value!.IsOnChain.Should().BeFalse();
        result.Value!.AsaInfo.Should().BeNull();
        result.Value!.ChainVerified.Should().BeFalse();
    }

    // ========================================================================
    // GetCredentialWithChainDataAsync — Not Found
    // ========================================================================

    [Fact]
    public async Task GetCredentialWithChainDataAsync_NotFound_ReturnsNotFound()
    {
        // Arrange
        _credentialServiceMock
            .Setup(s => s.GetByIdAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.NotFound("Not found"));

        // Act
        var result = await _sut.GetCredentialWithChainDataAsync("nonexistent");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // ========================================================================
    // RevokeAndBurnAsync — Burn Fails (Graceful Degradation)
    // ========================================================================

    [Fact]
    public async Task RevokeAndBurnAsync_BurnFails_StillRevokesCredential()
    {
        // Arrange
        var credentialId = "cred-burn-fail";
        var credential = CreateCredentialDto(id: credentialId, assetId: "asa-fail", status: "ACTIVE");

        _credentialServiceMock
            .Setup(s => s.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(credential));

        _algorandServiceMock
            .Setup(s => s.BurnASAAsync("asa-fail", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Failure("Chain unreachable"));

        var revokedCredential = CreateCredentialDto(id: credentialId, status: "REVOKED");
        _credentialServiceMock
            .Setup(s => s.RevokeAsync(credentialId, It.IsAny<RevokeMembershipCredentialDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(revokedCredential));

        // Act
        var result = await _sut.RevokeAndBurnAsync(credentialId);

        // Assert - credential is still revoked even though burn failed
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("REVOKED");

        _credentialServiceMock.Verify(s => s.RevokeAsync(
            credentialId,
            It.Is<RevokeMembershipCredentialDto>(d => d.RevokeTxHash == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // UpgradeTierAsync — Not Found
    // ========================================================================

    [Fact]
    public async Task UpgradeTierAsync_NotFound_ReturnsNotFound()
    {
        // Arrange
        _credentialServiceMock
            .Setup(s => s.GetByIdAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.NotFound("Not found"));

        // Act
        var result = await _sut.UpgradeTierAsync("nonexistent", "GOLD");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // ========================================================================
    // GrantAndMintAsync — Guild Scope
    // ========================================================================

    [Fact]
    public async Task GrantAndMintAsync_GuildScope_SetsCorrectScopeInTheIssueRequest()
    {
        // Arrange
        var sut = CreateServiceWithIssuanceNode();
        var recipientAvatarId = Guid.NewGuid();
        var dto = new GrantMembershipCredentialDto
        {
            GuildId = "guild-1",
            UserId = "user-1",
            GrantedVia = "APPLICATION_APPROVED"
        };

        var grantedCredential = CreateCredentialDto(id: "cred-guild", guildId: "guild-1", userId: "user-1");
        _credentialServiceMock
            .Setup(s => s.GrantAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(grantedCredential));

        GivenProvisionedAccount("user-1", recipientAvatarId);

        _algorandServiceMock
            .Setup(s => s.BuildARC19MetadataAsync(It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("{\"standard\":\"arc19\"}"));

        _issuanceNodeMock
            .Setup(s => s.IssueAsync(
                It.IsAny<Guid>(),
                It.IsAny<MembershipCredentialIssueRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialIssuance>.Success(new MembershipCredentialIssuance(
                IssuanceReference: "issuance-guild",
                ProofReference: "proof-guild",
                MetadataUri: null,
                IssuedAt: DateTimeOffset.UtcNow)));

        var updatedCredential = CreateCredentialDto(id: "cred-guild", guildId: "guild-1", assetId: "proof-guild");
        _credentialServiceMock
            .Setup(s => s.UpdateMintInfoAsync("cred-guild", It.IsAny<UpdateMembershipCredentialMintDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(updatedCredential));

        // Act
        var result = await sut.GrantAndMintAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _issuanceNodeMock.Verify(s => s.IssueAsync(
            recipientAvatarId,
            It.Is<MembershipCredentialIssueRequest>(r => r.Scope == "GUILD" && r.ScopeId == "guild-1"),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static MembershipCredentialDto CreateCredentialDto(
        string? id = null,
        string? projectId = null,
        string? guildId = null,
        string? userId = null,
        string? assetId = null,
        string status = "ACTIVE",
        string grantedVia = "FOUNDER",
        string? tier = null,
        string? mintTxHash = null,
        string? revokeTxHash = null,
        string? metadataUri = null)
    {
        return new MembershipCredentialDto
        {
            Id = id ?? Guid.NewGuid().ToString(),
            ProjectId = projectId,
            GuildId = guildId,
            UserId = userId ?? Guid.NewGuid().ToString(),
            AssetId = assetId,
            Status = status,
            IsTransferable = false,
            Tier = tier,
            GrantedVia = grantedVia,
            MetadataUri = metadataUri,
            MintTxHash = mintTxHash,
            RevokeTxHash = revokeTxHash,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
