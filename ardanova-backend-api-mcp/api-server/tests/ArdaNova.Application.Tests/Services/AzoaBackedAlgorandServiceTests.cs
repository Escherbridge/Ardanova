namespace ArdaNova.Application.Tests.Services;

using System.Text.Json;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Infrastructure.Azoa;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

/// <summary>
/// Unit tests for <see cref="AzoaBackedAlgorandService"/> — the AZOA-backed
/// implementation of the <see cref="IAlgorandService"/> seam (provider-adapter
/// track). Mocks <see cref="IAzoaNodeClient"/> directly (no HttpClient): the node
/// client owns transport, so these tests assert the adapter's mapping decisions.
///
/// Coverage mirrors the three honest buckets in the adapter:
///   - CLEAN MAP: CreateFungibleASAAsync routes to the fungible-mint call;
///                BuildARC19MetadataAsync output is byte-identical to legacy.
///   - DOCUMENTED GAP: address-keyed mint/transfer/read/ownership return a clear
///                Failure (and never throw).
///   - DEFERRED: Burn/Clawback return the deferred-to-H2 failure.
/// </summary>
public class AzoaBackedAlgorandServiceTests
{
    private const string FungibleMintPath = "/api/nft/fungible-mint";

    private readonly Mock<IAzoaNodeClient> _nodeMock;
    private readonly AzoaSettings _settings;
    private readonly Mock<ILogger<AzoaBackedAlgorandService>> _loggerMock;
    private readonly IAlgorandService _sut;

    public AzoaBackedAlgorandServiceTests()
    {
        _nodeMock = new Mock<IAzoaNodeClient>(MockBehavior.Strict);
        _settings = new AzoaSettings
        {
            BaseUrl = "https://azoa.test",
            ValueApiKey = "test-value-key",
            Mode = "Simulated",
            ChainType = "Algorand",
        };
        _loggerMock = new Mock<ILogger<AzoaBackedAlgorandService>>();

        _sut = new AzoaBackedAlgorandService(
            _nodeMock.Object,
            Options.Create(_settings),
            _loggerMock.Object);
    }

    // ========================================================================
    // CreateFungibleASAAsync — CLEAN MAP (routes to fungible-mint)
    // ========================================================================

    [Fact]
    public async Task CreateFungibleASAAsync_RoutesToFungibleMintCall_AndMapsResult()
    {
        // Arrange
        AzoaFungibleMintRequest? captured = null;
        _nodeMock
            .Setup(n => n.PostAsync<AzoaFungibleMintResult>(
                FungibleMintPath,
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, object?, CancellationToken>((_, body, _) =>
                captured = body as AzoaFungibleMintRequest)
            .ReturnsAsync(Result<AzoaFungibleMintResult>.Success(new AzoaFungibleMintResult
            {
                AssetId = "67890",
                TxHash = "FUNGIBLE_TX",
                OperationId = "op-1",
            }));

        // Act
        var result = await _sut.CreateFungibleASAAsync("ARDA Token", "ARDA", 1_000_000_000);

        // Assert — routed to the fungible-mint endpoint exactly once.
        _nodeMock.Verify(n => n.PostAsync<AzoaFungibleMintResult>(
            FungibleMintPath,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Request shape: name/unit pass through; supply is an opaque string (§1, §3).
        captured.Should().NotBeNull();
        captured!.Name.Should().Be("ARDA Token");
        captured.UnitName.Should().Be("ARDA");
        captured.TotalSupply.Should().Be("1000000000");
        captured.ChainType.Should().Be("Algorand");

        // Result projected onto the legacy FungibleAsaCreateResult shape.
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AssetId.Should().Be("67890");
        result.Value!.TxHash.Should().Be("FUNGIBLE_TX");
    }

    [Fact]
    public async Task CreateFungibleASAAsync_WhenTxHashMissing_FallsBackToOperationId()
    {
        // Arrange — Simulated mode: no chain tx hash, only a deterministic op id.
        _nodeMock
            .Setup(n => n.PostAsync<AzoaFungibleMintResult>(
                FungibleMintPath,
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaFungibleMintResult>.Success(new AzoaFungibleMintResult
            {
                AssetId = "sim:42",
                TxHash = null,
                OperationId = "sim-op-99",
            }));

        // Act
        var result = await _sut.CreateFungibleASAAsync("Proj Token", "PROJ", 1000);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AssetId.Should().Be("sim:42");
        result.Value!.TxHash.Should().Be("sim-op-99");
    }

    [Fact]
    public async Task CreateFungibleASAAsync_WhenNodeForbids_PreservesForbiddenType()
    {
        // Arrange — fail-closed KYC must survive the adapter boundary.
        _nodeMock
            .Setup(n => n.PostAsync<AzoaFungibleMintResult>(
                FungibleMintPath,
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaFungibleMintResult>.Forbidden("KYC_FORBIDDEN: not approved"));

        // Act
        var result = await _sut.CreateFungibleASAAsync("ARDA Token", "ARDA", 1000);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Contain("KYC_FORBIDDEN");
    }

    [Fact]
    public async Task CreateFungibleASAAsync_WhenNodeReturnsEmptyAssetId_ReturnsFailure()
    {
        // Arrange
        _nodeMock
            .Setup(n => n.PostAsync<AzoaFungibleMintResult>(
                FungibleMintPath,
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaFungibleMintResult>.Success(new AzoaFungibleMintResult
            {
                AssetId = "",
                OperationId = "op-1",
            }));

        // Act
        var result = await _sut.CreateFungibleASAAsync("ARDA Token", "ARDA", 1000);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
    }

    // ========================================================================
    // BuildARC19MetadataAsync — CLEAN MAP (byte-identical to legacy)
    // ========================================================================

    [Fact]
    public async Task BuildARC19MetadataAsync_OutputIsByteIdenticalToLegacyLogic()
    {
        // Arrange
        var input = CreateTestMetadataInput();
        var expected = BuildLegacyArc19Metadata(input); // same logic as legacy AlgorandService

        // Act
        var result = await _sut.BuildARC19MetadataAsync(input);

        // Assert — exact string match (byte-identical, including indentation).
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expected);

        // No node call for the pure metadata path.
        _nodeMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BuildARC19MetadataAsync_WithNullTier_IsByteIdenticalToLegacyLogic()
    {
        // Arrange
        var input = CreateTestMetadataInput(tier: null);
        var expected = BuildLegacyArc19Metadata(input);

        // Act
        var result = await _sut.BuildARC19MetadataAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expected);
    }

    [Fact]
    public async Task BuildARC19MetadataAsync_WithGuildScope_IsByteIdenticalToLegacyLogic()
    {
        // Arrange
        var input = CreateTestMetadataInput(scope: "GUILD", scopeName: "TestGuild");
        var expected = BuildLegacyArc19Metadata(input);

        // Act
        var result = await _sut.BuildARC19MetadataAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expected);

        // Sanity: still valid ARC-19 with the expected description.
        var doc = JsonDocument.Parse(result.Value!);
        doc.RootElement.GetProperty("standard").GetString().Should().Be("arc19");
        doc.RootElement.GetProperty("description").GetString().Should().Contain("TestGuild");
    }

    // ========================================================================
    // DOCUMENTED GAP — address-keyed calls return clear Failure (never throw)
    // ========================================================================

    [Fact]
    public async Task MintSoulboundASAAsync_ReturnsDocumentedGapFailure()
    {
        // Act
        var result = await _sut.MintSoulboundASAAsync("RECIPIENTADDR", CreateTestMetadataInput());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
        result.Error.Should().Contain("avatar");
        result.Error.Should().Contain("allocation service");
        _nodeMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TransferASAAsync_ReturnsDocumentedGapFailure()
    {
        // Act
        var result = await _sut.TransferASAAsync("12345", "RECIPIENTADDR", 100);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
        result.Error.Should().Contain("avatar");
        _nodeMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetASAInfoAsync_ReturnsDocumentedGapFailure()
    {
        // Act
        var result = await _sut.GetASAInfoAsync("12345");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
        result.Error.Should().Contain("avatar");
        _nodeMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetASABalanceAsync_ReturnsDocumentedGapFailure()
    {
        // Act
        var result = await _sut.GetASABalanceAsync("12345", "HOLDERADDR");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
        result.Error.Should().Contain("avatar");
        _nodeMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task VerifyOwnershipAsync_ReturnsDocumentedGapFailure()
    {
        // Act
        var result = await _sut.VerifyOwnershipAsync("12345", "HOLDERADDR");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
        result.Error.Should().Contain("avatar");
        _nodeMock.VerifyNoOtherCalls();
    }

    // ========================================================================
    // DEFERRED — Burn/Clawback return the deferred-to-H2 failure
    // ========================================================================

    [Fact]
    public async Task BurnASAAsync_ReturnsDeferredFailure()
    {
        // Act
        var result = await _sut.BurnASAAsync("12345");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
        result.Error.Should().Contain("deferred to AZOA H2");
        _nodeMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ClawbackASAAsync_ReturnsDeferredFailure()
    {
        // Act
        var result = await _sut.ClawbackASAAsync("12345", "FROMADDR", 50);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
        result.Error.Should().Contain("deferred to AZOA H2");
        _nodeMock.VerifyNoOtherCalls();
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static CredentialMetadataInput CreateTestMetadataInput(
        string scope = "PROJECT",
        string scopeName = "TestProject",
        string? tier = "GOLD")
    {
        return new CredentialMetadataInput
        {
            CredentialId = "cred_123",
            Scope = scope,
            ScopeId = "proj_456",
            ScopeName = scopeName,
            UserId = "user_789",
            Tier = tier,
            GrantedVia = "FOUNDER",
            GrantedAt = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
        };
    }

    /// <summary>
    /// Reproduces the legacy AlgorandService.BuildARC19MetadataAsync body exactly,
    /// so the adapter's output can be asserted byte-identical. If the legacy impl
    /// changes, this and the adapter must change together (and this test guards it).
    /// </summary>
    private static string BuildLegacyArc19Metadata(CredentialMetadataInput credential)
    {
        var metadata = new
        {
            standard = "arc19",
            name = "ArdaNova Membership Credential",
            description = $"Soulbound governance credential for {credential.ScopeName}",
            properties = new
            {
                credentialId = credential.CredentialId,
                scope = credential.Scope,
                scopeId = credential.ScopeId,
                scopeName = credential.ScopeName,
                userId = credential.UserId,
                tier = credential.Tier,
                grantedVia = credential.GrantedVia,
                grantedAt = credential.GrantedAt.ToString("o"),
                isTransferable = false,
                platform = "ArdaNova",
                version = "1.0"
            }
        };

        return JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
