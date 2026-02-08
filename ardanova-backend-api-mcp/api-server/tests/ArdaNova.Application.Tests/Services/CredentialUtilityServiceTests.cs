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
    private readonly CredentialUtilityService _sut;

    private const string TestPlatformAddress = "ALGO_TEST_PLATFORM_ADDRESS";

    public CredentialUtilityServiceTests()
    {
        _credentialServiceMock = new Mock<IMembershipCredentialService>();
        _algorandServiceMock = new Mock<IAlgorandService>();
        _loggerMock = new Mock<ILogger<CredentialUtilityService>>();

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Algorand:PlatformAddress"]).Returns(TestPlatformAddress);

        _sut = new CredentialUtilityService(
            _credentialServiceMock.Object,
            _algorandServiceMock.Object,
            configMock.Object,
            _loggerMock.Object);
    }

    // ========================================================================
    // GrantAndMintAsync — Happy Path
    // ========================================================================

    [Fact]
    public async Task GrantAndMintAsync_HappyPath_GrantsCredentialAndMintsASA()
    {
        // Arrange
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

        _algorandServiceMock
            .Setup(s => s.BuildARC19MetadataAsync(It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("{\"standard\":\"arc19\"}"));

        _algorandServiceMock
            .Setup(s => s.MintSoulboundASAAsync(It.IsAny<string>(), It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SoulboundAsaMintResult>.Success(new SoulboundAsaMintResult { AssetId = "asa-123", TxHash = "tx-mint-abc" }));

        var updatedCredential = CreateCredentialDto(id: "cred-1", projectId: "project-1", userId: "user-1", assetId: "asa-123", mintTxHash: "tx-mint-abc");
        _credentialServiceMock
            .Setup(s => s.UpdateMintInfoAsync("cred-1", It.IsAny<UpdateMembershipCredentialMintDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(updatedCredential));

        // Act
        var result = await _sut.GrantAndMintAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be("cred-1");
        result.Value!.AssetId.Should().Be("asa-123");
        result.Value!.MintTxHash.Should().Be("tx-mint-abc");

        _credentialServiceMock.Verify(s => s.GrantAsync(dto, It.IsAny<CancellationToken>()), Times.Once);
        _algorandServiceMock.Verify(s => s.MintSoulboundASAAsync(
            It.IsAny<string>(),
            It.Is<CredentialMetadataInput>(m => m.Scope == "PROJECT" && m.ScopeId == "project-1"),
            It.IsAny<CancellationToken>()), Times.Once);
        _credentialServiceMock.Verify(s => s.UpdateMintInfoAsync(
            "cred-1",
            It.Is<UpdateMembershipCredentialMintDto>(d => d.AssetId == "asa-123" && d.MintTxHash == "tx-mint-abc"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // GrantAndMintAsync — Chain Failure (Graceful Degradation)
    // ========================================================================

    [Fact]
    public async Task GrantAndMintAsync_ChainFailure_ReturnsCredentialWithoutASA()
    {
        // Arrange
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

        _algorandServiceMock
            .Setup(s => s.BuildARC19MetadataAsync(It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("{\"standard\":\"arc19\"}"));

        _algorandServiceMock
            .Setup(s => s.MintSoulboundASAAsync(It.IsAny<string>(), It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SoulboundAsaMintResult>.Failure("Algorand node unreachable"));

        // Act
        var result = await _sut.GrantAndMintAsync(dto);

        // Assert - credential is still returned (graceful degradation)
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be("cred-2");
        result.Value!.AssetId.Should().BeNull("chain was down so no ASA was minted");

        _credentialServiceMock.Verify(s => s.UpdateMintInfoAsync(
            It.IsAny<string>(), It.IsAny<UpdateMembershipCredentialMintDto>(), It.IsAny<CancellationToken>()), Times.Never);
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
    // CheckAndAutoGrantAsync — Eligible, Grants And Mints
    // ========================================================================

    [Fact]
    public async Task CheckAndAutoGrantAsync_Eligible_GrantsAndMintsCredential()
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

        _algorandServiceMock
            .Setup(s => s.BuildARC19MetadataAsync(It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("{\"standard\":\"arc19\"}"));

        _algorandServiceMock
            .Setup(s => s.MintSoulboundASAAsync(It.IsAny<string>(), It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SoulboundAsaMintResult>.Success(new SoulboundAsaMintResult { AssetId = "asa-auto", TxHash = "tx-auto" }));

        var updatedCredential = CreateCredentialDto(id: "cred-auto", projectId: "project-1", userId: "user-1", assetId: "asa-auto");
        _credentialServiceMock
            .Setup(s => s.UpdateMintInfoAsync("cred-auto", It.IsAny<UpdateMembershipCredentialMintDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(updatedCredential));

        // Act
        var result = await _sut.CheckAndAutoGrantAsync("user-1", "project-1", null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be("cred-auto");
    }

    // ========================================================================
    // RetryMintAsync — Happy Path
    // ========================================================================

    [Fact]
    public async Task RetryMintAsync_CredentialWithoutASA_MintsSuccessfully()
    {
        // Arrange
        var credentialId = "cred-retry";
        var credential = CreateCredentialDto(id: credentialId, projectId: "project-1", userId: "user-1", assetId: null, status: "ACTIVE");

        _credentialServiceMock
            .Setup(s => s.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(credential));

        _algorandServiceMock
            .Setup(s => s.BuildARC19MetadataAsync(It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("{\"standard\":\"arc19\"}"));

        _algorandServiceMock
            .Setup(s => s.MintSoulboundASAAsync(It.IsAny<string>(), It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SoulboundAsaMintResult>.Success(new SoulboundAsaMintResult { AssetId = "asa-retry", TxHash = "tx-retry" }));

        var updatedCredential = CreateCredentialDto(id: credentialId, projectId: "project-1", assetId: "asa-retry", mintTxHash: "tx-retry");
        _credentialServiceMock
            .Setup(s => s.UpdateMintInfoAsync(credentialId, It.IsAny<UpdateMembershipCredentialMintDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(updatedCredential));

        // Act
        var result = await _sut.RetryMintAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AssetId.Should().Be("asa-retry");
        result.Value!.MintTxHash.Should().Be("tx-retry");
    }

    // ========================================================================
    // RetryMintAsync — Already Minted
    // ========================================================================

    [Fact]
    public async Task RetryMintAsync_AlreadyMinted_ReturnsValidationError()
    {
        // Arrange
        var credentialId = "cred-already";
        var credential = CreateCredentialDto(id: credentialId, assetId: "asa-exists", status: "ACTIVE");

        _credentialServiceMock
            .Setup(s => s.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(credential));

        // Act
        var result = await _sut.RetryMintAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("already minted");
    }

    // ========================================================================
    // RetryMintAsync — Not Active
    // ========================================================================

    [Fact]
    public async Task RetryMintAsync_NotActive_ReturnsValidationError()
    {
        // Arrange
        var credentialId = "cred-revoked";
        var credential = CreateCredentialDto(id: credentialId, assetId: null, status: "REVOKED");

        _credentialServiceMock
            .Setup(s => s.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(credential));

        // Act
        var result = await _sut.RetryMintAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("ACTIVE");
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
    public async Task GrantAndMintAsync_GuildScope_SetsCorrectScopeInMetadata()
    {
        // Arrange
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

        _algorandServiceMock
            .Setup(s => s.BuildARC19MetadataAsync(It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("{\"standard\":\"arc19\"}"));

        _algorandServiceMock
            .Setup(s => s.MintSoulboundASAAsync(It.IsAny<string>(), It.IsAny<CredentialMetadataInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SoulboundAsaMintResult>.Success(new SoulboundAsaMintResult { AssetId = "asa-guild", TxHash = "tx-guild" }));

        var updatedCredential = CreateCredentialDto(id: "cred-guild", guildId: "guild-1", assetId: "asa-guild");
        _credentialServiceMock
            .Setup(s => s.UpdateMintInfoAsync("cred-guild", It.IsAny<UpdateMembershipCredentialMintDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MembershipCredentialDto>.Success(updatedCredential));

        // Act
        var result = await _sut.GrantAndMintAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _algorandServiceMock.Verify(s => s.MintSoulboundASAAsync(
            It.IsAny<string>(),
            It.Is<CredentialMetadataInput>(m => m.Scope == "GUILD" && m.ScopeId == "guild-1"),
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
