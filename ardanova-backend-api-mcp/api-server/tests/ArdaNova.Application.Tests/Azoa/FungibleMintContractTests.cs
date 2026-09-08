namespace ArdaNova.Application.Tests.Azoa;

using System.Text.Json;
using ArdaNova.Application.Common.Results;
using ArdaNova.Infrastructure.Azoa;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

/// <summary>
/// Pins the wire shape of <c>POST /api/nft/fungible-mint</c> against AZOA's real
/// DTOs — <c>Models/Requests/FungibleMintRequest.cs</c> and
/// <c>Models/Responses/FungibleTokenResult.cs</c> in the AZOA repo (oasis-sleek).
/// ArdaNova cannot build or run the AZOA solution from this repo, so these tests
/// are the closest buildable stand-in for an OpenAPI schema diff: they assert the
/// exact JSON property names and CLR types AZOA's controller actually binds/emits,
/// literally transcribed from its source at the time this test was written. If
/// AZOA's DTO shape changes, this test (and the wire models it pins) must change
/// with it — that drift is the whole point of the guard.
///
/// The bug this closes: <see cref="AzoaFungibleMintRequest"/> used to serialize
/// total supply as an opaque JSON string (<c>"totalSupply"</c>), mirroring the
/// allocation door's convention. AZOA's <c>FungibleMintRequest.Total</c> is a real
/// <c>ulong</c> bound from a JSON number named <c>"total"</c>; a string body
/// silently deserialized to <c>Total = 0</c> and the node rejected the call with
/// "zero total" on every single mint once <c>Algorand:Provider</c> was set to
/// <c>Azoa</c>.
/// </summary>
public class FungibleMintContractTests
{
    private static readonly JsonSerializerOptions WireJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    // ========================================================================
    // Request shape — must match AZOA's FungibleMintRequest exactly
    // ========================================================================

    [Fact]
    public void AzoaFungibleMintRequest_SerializesTotalAsANumber_NotAnOpaqueString()
    {
        // Arrange
        var request = new AzoaFungibleMintRequest
        {
            ChainType = "Algorand",
            Name = "ARDA Token",
            UnitName = "ARDA",
            Total = 1_000_000_000UL,
            Decimals = 6,
        };

        // Act
        var json = JsonSerializer.Serialize(request);
        using var doc = JsonDocument.Parse(json);

        // Assert — "total" is a JSON number, matching AZOA's ulong-typed field.
        doc.RootElement.TryGetProperty("total", out var totalElement).Should().BeTrue();
        totalElement.ValueKind.Should().Be(JsonValueKind.Number);
        totalElement.GetUInt64().Should().Be(1_000_000_000UL);

        // Assert — the legacy opaque-string field name must never come back.
        doc.RootElement.TryGetProperty("totalSupply", out _).Should().BeFalse(
            "AZOA's FungibleMintRequest has no totalSupply field; a string body here " +
            "deserializes Total to 0 and every mint fails with \"zero total\"");
    }

    [Fact]
    public void AzoaFungibleMintRequest_SerializesDecimalsAsANumber()
    {
        // Arrange
        var request = new AzoaFungibleMintRequest
        {
            ChainType = "Algorand",
            Name = "Proj Token",
            UnitName = "PROJ",
            Total = 42,
            Decimals = 6,
        };

        // Act
        var json = JsonSerializer.Serialize(request);
        using var doc = JsonDocument.Parse(json);

        // Assert
        doc.RootElement.GetProperty("decimals").ValueKind.Should().Be(JsonValueKind.Number);
        doc.RootElement.GetProperty("decimals").GetInt32().Should().Be(6);
    }

    [Fact]
    public void AzoaFungibleMintRequest_JsonPropertyNames_MatchAzoaFungibleMintRequestExactly()
    {
        // Arrange — the full field set AZOA's controller binds for this route
        // (chainType, name, unitName, total, decimals; holonId is optional and
        // intentionally unset by this adapter — the legacy IAlgorandService
        // surface carries no holon context to link).
        var request = new AzoaFungibleMintRequest
        {
            ChainType = "Algorand",
            Name = "ARDA Token",
            UnitName = "ARDA",
            Total = 1,
            Decimals = 6,
        };

        // Act
        var json = JsonSerializer.Serialize(request);
        using var doc = JsonDocument.Parse(json);
        var actualPropertyNames = doc.RootElement.EnumerateObject()
            .Select(p => p.Name)
            .ToArray();

        // Assert — exactly the wire names AZOA's FungibleMintRequest binds.
        actualPropertyNames.Should().BeEquivalentTo(new[]
        {
            "chainType", "name", "unitName", "total", "decimals",
        });
    }

    [Fact]
    public void AzoaFungibleMintRequest_TotalProperty_IsUlongNotString()
    {
        // A reflection guard against a future regression back to an opaque
        // string amount (the exact shape of the original defect).
        var property = typeof(AzoaFungibleMintRequest).GetProperty(nameof(AzoaFungibleMintRequest.Total));

        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(ulong));
    }

    // ========================================================================
    // Response shape — must match AZOA's FungibleTokenResult exactly
    // ========================================================================

    [Fact]
    public void AzoaFungibleMintResult_DeserializesAzoaFungibleTokenResultShape()
    {
        // Arrange — literal transcription of AZOA's FungibleTokenResult wire
        // shape (Models/Responses/FungibleTokenResult.cs): userId, walletId,
        // walletAddress, walletProvisioned, assetId, replayed. No txHash, no
        // operationId — the platform reserve signs the ASA-create, so the node
        // never hands the caller a chain reference for this call.
        const string azoaResponseJson = """
            {
              "userId": "11111111-1111-1111-1111-111111111111",
              "walletId": "22222222-2222-2222-2222-222222222222",
              "walletAddress": "PLATFORMRESERVEADDR",
              "walletProvisioned": true,
              "assetId": "67890",
              "replayed": false
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<AzoaFungibleMintResult>(azoaResponseJson, WireJsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        result.WalletId.Should().Be(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        result.WalletAddress.Should().Be("PLATFORMRESERVEADDR");
        result.WalletProvisioned.Should().BeTrue();
        result.AssetId.Should().Be("67890");
        result.Replayed.Should().BeFalse();
    }

    [Fact]
    public void AzoaFungibleMintResult_HasNoTxHashOrOperationIdProperty()
    {
        // AZOA's FungibleTokenResult never carried these fields; the adapter's
        // old wire model read them anyway (they always deserialized to the CLR
        // default from a JSON payload that never sent them). Guard against
        // reintroducing a field that silently reads as empty forever.
        typeof(AzoaFungibleMintResult).GetProperty("TxHash").Should().BeNull();
        typeof(AzoaFungibleMintResult).GetProperty("OperationId").Should().BeNull();
    }

    // ========================================================================
    // End-to-end — the adapter method, wired against the pinned request/response
    // ========================================================================

    [Fact]
    public async Task CreateFungibleASAAsync_SendsRealTotalAndAssetScale_ToTheAzoaNode()
    {
        // Arrange
        var nodeMock = new Mock<IAzoaNodeClient>(MockBehavior.Strict);
        var settings = new AzoaSettings
        {
            BaseUrl = "https://azoa.test",
            ValueApiKey = "test-value-key",
            Mode = "Simulated",
            ChainType = "Algorand",
        };
        var loggerMock = new Mock<ILogger<AzoaBackedAlgorandService>>();
        var sut = new AzoaBackedAlgorandService(nodeMock.Object, Options.Create(settings), loggerMock.Object);

        AzoaFungibleMintRequest? captured = null;
        nodeMock
            .Setup(n => n.PostAsync<AzoaFungibleMintResult>(
                "/api/nft/fungible-mint",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, object?, CancellationToken>((_, body, _) => captured = body as AzoaFungibleMintRequest)
            .ReturnsAsync(Result<AzoaFungibleMintResult>.Success(new AzoaFungibleMintResult
            {
                UserId = Guid.NewGuid(),
                WalletId = Guid.NewGuid(),
                WalletAddress = "PLATFORMRESERVEADDR",
                WalletProvisioned = false,
                AssetId = "67890",
                Replayed = false,
            }));

        // Act
        var result = await sut.CreateFungibleASAAsync("ARDA Token", "ARDA", 1_000_000_000);

        // Assert — the node received a real number, at the asset scale the
        // legacy provider also uses (6 decimals), not an opaque string.
        captured.Should().NotBeNull();
        captured!.Total.Should().Be(1_000_000_000UL);
        captured.Decimals.Should().Be(6);
        captured.Name.Should().Be("ARDA Token");
        captured.UnitName.Should().Be("ARDA");
        captured.ChainType.Should().Be("Algorand");

        // Assert — mapped onto the legacy FungibleAsaCreateResult shape; no
        // fabricated transaction hash for a field AZOA's response never sends.
        result.IsSuccess.Should().BeTrue();
        result.Value!.AssetId.Should().Be("67890");
        result.Value!.TxHash.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateFungibleASAAsync_WhenNodeReturnsEmptyAssetId_ReturnsFailure()
    {
        // Arrange
        var nodeMock = new Mock<IAzoaNodeClient>(MockBehavior.Strict);
        var settings = new AzoaSettings { BaseUrl = "https://azoa.test", ValueApiKey = "k", Mode = "Simulated" };
        var loggerMock = new Mock<ILogger<AzoaBackedAlgorandService>>();
        var sut = new AzoaBackedAlgorandService(nodeMock.Object, Options.Create(settings), loggerMock.Object);

        nodeMock
            .Setup(n => n.PostAsync<AzoaFungibleMintResult>(
                "/api/nft/fungible-mint",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaFungibleMintResult>.Success(new AzoaFungibleMintResult { AssetId = "" }));

        // Act
        var result = await sut.CreateFungibleASAAsync("ARDA Token", "ARDA", 1000);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
    }
}
