namespace ArdaNova.Application.Tests.Services;

using System.Net;
using System.Text;
using System.Text.Json;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Infrastructure.Algorand;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

/// <summary>
/// Unit tests for AlgorandService.
/// Uses a mocked HttpMessageHandler to intercept HTTP calls to Algorand REST APIs.
/// </summary>
public class AlgorandServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly AlgorandSettings _settings;
    private readonly Mock<ILogger<AlgorandService>> _loggerMock;
    private readonly IAlgorandService _sut;

    public AlgorandServiceTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object);
        _settings = new AlgorandSettings
        {
            Network = "testnet",
            NodeUrl = "https://testnet-api.algonode.cloud",
            IndexerUrl = "https://testnet-idx.algonode.cloud",
            PlatformMnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon invest",
            PlatformAddress = "PLATFORMADDRESS"
        };
        _loggerMock = new Mock<ILogger<AlgorandService>>();

        _sut = new AlgorandService(
            Options.Create(_settings),
            _httpClient,
            _loggerMock.Object);
    }

    // ========================================================================
    // BuildARC19MetadataAsync
    // ========================================================================

    [Fact]
    public async Task BuildARC19MetadataAsync_WithValidInput_ReturnsValidJson()
    {
        // Arrange
        var input = CreateTestMetadataInput();

        // Act
        var result = await _sut.BuildARC19MetadataAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();

        var json = JsonDocument.Parse(result.Value!);
        var root = json.RootElement;

        root.GetProperty("standard").GetString().Should().Be("arc19");
        root.GetProperty("name").GetString().Should().Be("ArdaNova Membership Credential");

        var props = root.GetProperty("properties");
        props.GetProperty("credentialId").GetString().Should().Be(input.CredentialId);
        props.GetProperty("scope").GetString().Should().Be("PROJECT");
        props.GetProperty("scopeId").GetString().Should().Be(input.ScopeId);
        props.GetProperty("scopeName").GetString().Should().Be(input.ScopeName);
        props.GetProperty("userId").GetString().Should().Be(input.UserId);
        props.GetProperty("tier").GetString().Should().Be("GOLD");
        props.GetProperty("grantedVia").GetString().Should().Be("FOUNDER");
        props.GetProperty("isTransferable").GetBoolean().Should().BeFalse();
        props.GetProperty("platform").GetString().Should().Be("ArdaNova");
        props.GetProperty("version").GetString().Should().Be("1.0");
    }

    [Fact]
    public async Task BuildARC19MetadataAsync_WithNullTier_SetsTierToNull()
    {
        // Arrange
        var input = CreateTestMetadataInput(tier: null);

        // Act
        var result = await _sut.BuildARC19MetadataAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var json = JsonDocument.Parse(result.Value!);
        var props = json.RootElement.GetProperty("properties");
        props.GetProperty("tier").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task BuildARC19MetadataAsync_WithGuildScope_SetsDescriptionCorrectly()
    {
        // Arrange
        var input = CreateTestMetadataInput(scope: "GUILD", scopeName: "TestGuild");

        // Act
        var result = await _sut.BuildARC19MetadataAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var json = JsonDocument.Parse(result.Value!);
        var description = json.RootElement.GetProperty("description").GetString();
        description.Should().Contain("TestGuild");
    }

    // ========================================================================
    // MintSoulboundASAAsync
    // ========================================================================

    [Fact]
    public async Task MintSoulboundASAAsync_WhenNodeReturnsSuccess_ReturnsMintResult()
    {
        // Arrange
        var input = CreateTestMetadataInput();
        var recipientAddress = "RECIPIENTADDRESS";

        // Mock TransactionParams response
        SetupTransactionParamsResponse();

        // Mock ASA creation transaction response
        SetupSubmitTransactionResponse("TXID123");

        // Mock waiting for transaction confirmation with asset ID
        SetupPendingTransactionResponse("TXID123", assetIndex: 12345);

        // Act
        var result = await _sut.MintSoulboundASAAsync(recipientAddress, input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AssetId.Should().Be("12345");
        result.Value!.TxHash.Should().Be("TXID123");
    }

    [Fact]
    public async Task MintSoulboundASAAsync_WhenNodeIsUnreachable_ReturnsFailure()
    {
        // Arrange
        var input = CreateTestMetadataInput();
        var recipientAddress = "RECIPIENTADDRESS";

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act
        var result = await _sut.MintSoulboundASAAsync(recipientAddress, input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
        // The service wraps the HTTP error inside "Failed to get transaction parameters"
        // which is correct graceful degradation behavior
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task MintSoulboundASAAsync_WhenNodeReturnsError_ReturnsFailure()
    {
        // Arrange
        var input = CreateTestMetadataInput();
        var recipientAddress = "RECIPIENTADDRESS";

        // Mock TransactionParams response to return 500
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"message\": \"Internal error\"}")
            });

        // Act
        var result = await _sut.MintSoulboundASAAsync(recipientAddress, input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
    }

    // ========================================================================
    // BurnASAAsync
    // ========================================================================

    [Fact]
    public async Task BurnASAAsync_WhenNodeReturnsSuccess_ReturnsTxHash()
    {
        // Arrange
        var assetId = "12345";

        SetupTransactionParamsResponse();
        SetupSubmitTransactionResponse("BURN_TX_HASH");
        SetupPendingTransactionResponse("BURN_TX_HASH");

        // Act
        var result = await _sut.BurnASAAsync(assetId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("BURN_TX_HASH");
    }

    [Fact]
    public async Task BurnASAAsync_WhenNodeFails_ReturnsFailure()
    {
        // Arrange
        var assetId = "12345";

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _sut.BurnASAAsync(assetId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
    }

    // ========================================================================
    // GetASAInfoAsync
    // ========================================================================

    [Fact]
    public async Task GetASAInfoAsync_WhenAssetExists_ReturnsAsaInfo()
    {
        // Arrange
        var assetId = "12345";
        var indexerResponse = new
        {
            asset = new
            {
                index = 12345,
                @params = new
                {
                    name = "Test Credential",
                    total = 1UL,
                    decimals = 0,
                    creator = "CREATORADDR",
                    manager = "PLATFORMADDRESS",
                    freeze = "PLATFORMADDRESS",
                    clawback = "PLATFORMADDRESS",
                    reserve = "PLATFORMADDRESS",
                    url = "https://metadata.example.com/cred.json",
                    deleted = false,
                    @default_frozen = true,
                    unit_name = "CRED"
                }
            }
        };

        // The indexer uses kebab-case, let me build the JSON manually
        var jsonResponse = @"{
            ""asset"": {
                ""index"": 12345,
                ""params"": {
                    ""name"": ""Test Credential"",
                    ""unit-name"": ""CRED"",
                    ""total"": 1,
                    ""decimals"": 0,
                    ""default-frozen"": true,
                    ""url"": ""https://metadata.example.com/cred.json"",
                    ""manager"": ""PLATFORMADDRESS"",
                    ""freeze"": ""PLATFORMADDRESS"",
                    ""clawback"": ""PLATFORMADDRESS"",
                    ""reserve"": ""PLATFORMADDRESS"",
                    ""creator"": ""CREATORADDR"",
                    ""deleted"": false
                }
            }
        }";

        SetupIndexerResponse($"/v2/assets/{assetId}", jsonResponse);

        // Act
        var result = await _sut.GetASAInfoAsync(assetId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AssetId.Should().Be("12345");
        result.Value!.AssetName.Should().Be("Test Credential");
        result.Value!.UnitName.Should().Be("CRED");
        result.Value!.Total.Should().Be(1);
        result.Value!.Decimals.Should().Be(0);
        result.Value!.DefaultFrozen.Should().BeTrue();
        result.Value!.ManagerAddress.Should().Be("PLATFORMADDRESS");
        result.Value!.ClawbackAddress.Should().Be("PLATFORMADDRESS");
        result.Value!.CreatorAddress.Should().Be("CREATORADDR");
        result.Value!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetASAInfoAsync_WhenAssetNotFound_ReturnsNotFound()
    {
        // Arrange
        var assetId = "99999";

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.RequestUri != null && r.RequestUri.ToString().Contains($"/v2/assets/{assetId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"message\": \"asset not found\"}")
            });

        // Act
        var result = await _sut.GetASAInfoAsync(assetId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // ========================================================================
    // VerifyOwnershipAsync
    // ========================================================================

    [Fact]
    public async Task VerifyOwnershipAsync_WhenAddressHoldsAsset_ReturnsTrue()
    {
        // Arrange
        var assetId = "12345";
        var address = "HOLDERADDRESS";

        var jsonResponse = @"{
            ""account"": {
                ""address"": ""HOLDERADDRESS"",
                ""assets"": [
                    { ""asset-id"": 12345, ""amount"": 1 }
                ]
            }
        }";

        SetupIndexerResponse($"/v2/accounts/{address}", jsonResponse);

        // Act
        var result = await _sut.VerifyOwnershipAsync(assetId, address);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyOwnershipAsync_WhenAddressDoesNotHoldAsset_ReturnsFalse()
    {
        // Arrange
        var assetId = "12345";
        var address = "OTHERADDRESS";

        var jsonResponse = @"{
            ""account"": {
                ""address"": ""OTHERADDRESS"",
                ""assets"": [
                    { ""asset-id"": 99999, ""amount"": 1 }
                ]
            }
        }";

        SetupIndexerResponse($"/v2/accounts/{address}", jsonResponse);

        // Act
        var result = await _sut.VerifyOwnershipAsync(assetId, address);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyOwnershipAsync_WhenAddressHasNoAssets_ReturnsFalse()
    {
        // Arrange
        var assetId = "12345";
        var address = "EMPTYADDRESS";

        var jsonResponse = @"{
            ""account"": {
                ""address"": ""EMPTYADDRESS"",
                ""assets"": []
            }
        }";

        SetupIndexerResponse($"/v2/accounts/{address}", jsonResponse);

        // Act
        var result = await _sut.VerifyOwnershipAsync(assetId, address);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    // ========================================================================
    // CreateFungibleASAAsync (Track 09 support)
    // ========================================================================

    [Fact]
    public async Task CreateFungibleASAAsync_WhenSuccess_ReturnsAssetIdAndTxHash()
    {
        // Arrange
        SetupTransactionParamsResponse();
        SetupSubmitTransactionResponse("FUNGIBLE_TX");
        SetupPendingTransactionResponse("FUNGIBLE_TX", assetIndex: 67890);

        // Act
        var result = await _sut.CreateFungibleASAAsync("ARDA Token", "ARDA", 1_000_000_000);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AssetId.Should().Be("67890");
        result.Value!.TxHash.Should().Be("FUNGIBLE_TX");
    }

    [Fact]
    public async Task CreateFungibleASAAsync_WhenNetworkFails_ReturnsFailure()
    {
        // Arrange
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Timeout"));

        // Act
        var result = await _sut.CreateFungibleASAAsync("ARDA Token", "ARDA", 1_000_000_000);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
    }

    // ========================================================================
    // TransferASAAsync (Track 09 support)
    // ========================================================================

    [Fact]
    public async Task TransferASAAsync_WhenSuccess_ReturnsTxHash()
    {
        // Arrange
        SetupTransactionParamsResponse();
        SetupSubmitTransactionResponse("TRANSFER_TX");
        SetupPendingTransactionResponse("TRANSFER_TX");

        // Act
        var result = await _sut.TransferASAAsync("12345", "RECIPIENTADDR", 100);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("TRANSFER_TX");
    }

    [Fact]
    public async Task TransferASAAsync_WhenNetworkFails_ReturnsFailure()
    {
        // Arrange
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection error"));

        // Act
        var result = await _sut.TransferASAAsync("12345", "RECIPIENTADDR", 100);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    // ========================================================================
    // GetASABalanceAsync (Track 09 support)
    // ========================================================================

    [Fact]
    public async Task GetASABalanceAsync_WhenAccountHoldsAsset_ReturnsBalance()
    {
        // Arrange
        var assetId = "12345";
        var address = "HOLDERADDRESS";

        var jsonResponse = @"{
            ""account"": {
                ""address"": ""HOLDERADDRESS"",
                ""assets"": [
                    { ""asset-id"": 12345, ""amount"": 500 }
                ]
            }
        }";

        SetupIndexerResponse($"/v2/accounts/{address}", jsonResponse);

        // Act
        var result = await _sut.GetASABalanceAsync(assetId, address);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(500UL);
    }

    [Fact]
    public async Task GetASABalanceAsync_WhenAccountDoesNotHoldAsset_ReturnsZero()
    {
        // Arrange
        var assetId = "12345";
        var address = "EMPTYADDRESS";

        var jsonResponse = @"{
            ""account"": {
                ""address"": ""EMPTYADDRESS"",
                ""assets"": []
            }
        }";

        SetupIndexerResponse($"/v2/accounts/{address}", jsonResponse);

        // Act
        var result = await _sut.GetASABalanceAsync(assetId, address);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0UL);
    }

    // ========================================================================
    // ClawbackASAAsync (Track 09 support)
    // ========================================================================

    [Fact]
    public async Task ClawbackASAAsync_WhenSuccess_ReturnsTxHash()
    {
        // Arrange
        SetupTransactionParamsResponse();
        SetupSubmitTransactionResponse("CLAWBACK_TX");
        SetupPendingTransactionResponse("CLAWBACK_TX");

        // Act
        var result = await _sut.ClawbackASAAsync("12345", "FROMADDRESS", 50);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("CLAWBACK_TX");
    }

    [Fact]
    public async Task ClawbackASAAsync_WhenNetworkFails_ReturnsFailure()
    {
        // Arrange
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _sut.ClawbackASAAsync("12345", "FROMADDRESS", 50);

        // Assert
        result.IsSuccess.Should().BeFalse();
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
            GrantedAt = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    private void SetupTransactionParamsResponse()
    {
        var transParamsJson = @"{
            ""consensus-version"": ""v38"",
            ""fee"": 1000,
            ""genesis-hash"": ""SGO1GKSzyE7IEPItTxCByw9x8FmnrCDexi9/cOUJOiI="",
            ""genesis-id"": ""testnet-v1.0"",
            ""last-round"": 30000000,
            ""min-fee"": 1000
        }";

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.RequestUri != null && r.RequestUri.ToString().Contains("/v2/transactions/params")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(transParamsJson, Encoding.UTF8, "application/json")
            });
    }

    private void SetupSubmitTransactionResponse(string txId)
    {
        var submitJson = $"{{\"txId\": \"{txId}\"}}";

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.RequestUri != null && r.RequestUri.ToString().Contains("/v2/transactions") &&
                    r.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(submitJson, Encoding.UTF8, "application/json")
            });
    }

    private void SetupPendingTransactionResponse(string txId, ulong? assetIndex = null)
    {
        var assetIndexJson = assetIndex.HasValue
            ? $", \"asset-index\": {assetIndex.Value}"
            : "";

        var pendingJson = $@"{{
            ""confirmed-round"": 30000001,
            ""pool-error"": """"
            {assetIndexJson}
        }}";

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.RequestUri != null && r.RequestUri.ToString().Contains($"/v2/transactions/pending/{txId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(pendingJson, Encoding.UTF8, "application/json")
            });
    }

    private void SetupIndexerResponse(string pathContains, string jsonResponse)
    {
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.RequestUri != null && r.RequestUri.ToString().Contains(pathContains)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });
    }
}
