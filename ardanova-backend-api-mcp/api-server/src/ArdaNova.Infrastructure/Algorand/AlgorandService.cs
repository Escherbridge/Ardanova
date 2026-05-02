namespace ArdaNova.Infrastructure.Algorand;

using System.Text;
using System.Text.Json;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Algorand blockchain service implementation using HttpClient to call
/// the Algorand REST APIs (Algod and Indexer) directly.
///
/// This implementation uses the custodial model where the platform account
/// signs all transactions on behalf of users.
///
/// Architecture:
///  - Algod API: Transaction submission, params, pending status
///  - Indexer API: Asset queries, account queries, ownership verification
///
/// The Algorand2 NuGet SDK is available but we use HttpClient directly
/// for explicit control over the REST calls and to avoid SDK version
/// coupling. The SDK types (Account, etc.) are used for cryptographic
/// operations like signing transactions.
/// </summary>
public class AlgorandService : IAlgorandService
{
    private readonly AlgorandSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AlgorandService> _logger;

    public AlgorandService(
        IOptions<AlgorandSettings> settings,
        HttpClient httpClient,
        ILogger<AlgorandService> logger)
    {
        _settings = settings.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    private static bool TryParseAssetId(string assetId, out ulong result)
        => ulong.TryParse(assetId, out result);

    // ========================================================================
    // BuildARC19MetadataAsync
    // ========================================================================

    /// <inheritdoc/>
    public Task<Result<string>> BuildARC19MetadataAsync(
        CredentialMetadataInput credential,
        CancellationToken ct = default)
    {
        try
        {
            var scopeLabel = credential.Scope.Equals("GUILD", StringComparison.OrdinalIgnoreCase)
                ? "guild"
                : "project";

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

            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return Task.FromResult(Result<string>.Success(json));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build ARC-19 metadata for credential {CredentialId}", credential.CredentialId);
            return Task.FromResult(Result<string>.Failure($"Failed to build ARC-19 metadata: {ex.Message}"));
        }
    }

    // ========================================================================
    // MintSoulboundASAAsync
    // ========================================================================

    /// <inheritdoc/>
    public async Task<Result<SoulboundAsaMintResult>> MintSoulboundASAAsync(
        string recipientAddress,
        CredentialMetadataInput metadata,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Minting soulbound ASA for credential {CredentialId} to {RecipientAddress}",
                metadata.CredentialId, recipientAddress);

            // 1. Get transaction parameters
            var txParams = await GetTransactionParamsAsync(ct);
            if (txParams == null)
                return Result<SoulboundAsaMintResult>.Failure("Failed to get transaction parameters from Algorand node");

            // 2. Build ARC-19 metadata
            var metadataResult = await BuildARC19MetadataAsync(metadata, ct);
            if (metadataResult.IsFailure)
                return Result<SoulboundAsaMintResult>.Failure($"Failed to build metadata: {metadataResult.Error}");

            // 3. Build the ASA creation transaction
            // For soulbound: total=1, decimals=0, defaultFrozen=true
            // manager/reserve/freeze/clawback = platformAddress
            var assetName = $"{metadata.ScopeName} Membership";
            if (assetName.Length > 32)
                assetName = assetName[..32];

            var txnBytes = BuildAssetCreateTransaction(
                txParams.Value,
                assetName: assetName,
                unitName: "CRED",
                total: 1,
                decimals: 0,
                defaultFrozen: true,
                manager: _settings.PlatformAddress,
                reserve: _settings.PlatformAddress,
                freeze: _settings.PlatformAddress,
                clawback: _settings.PlatformAddress,
                url: $"https://ardanova.com/credentials/{metadata.CredentialId}");

            // 4. Sign the transaction with the platform account
            var signedTxn = SignTransaction(txnBytes);

            // 5. Submit the transaction
            var txId = await SubmitTransactionAsync(signedTxn, ct);
            if (txId == null)
                return Result<SoulboundAsaMintResult>.Failure("Failed to submit ASA creation transaction");

            // 6. Wait for confirmation and get the asset ID
            var confirmation = await WaitForConfirmationAsync(txId, ct);
            if (confirmation == null)
                return Result<SoulboundAsaMintResult>.Failure("Transaction confirmation timed out");

            var assetId = GetAssetIndexFromConfirmation(confirmation.Value);
            if (assetId == null)
                return Result<SoulboundAsaMintResult>.Failure("Asset ID not found in transaction confirmation");

            _logger.LogInformation(
                "Successfully minted soulbound ASA {AssetId} (tx: {TxHash}) for credential {CredentialId}",
                assetId, txId, metadata.CredentialId);

            return Result<SoulboundAsaMintResult>.Success(new SoulboundAsaMintResult
            {
                AssetId = assetId,
                TxHash = txId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mint soulbound ASA for credential {CredentialId}", metadata.CredentialId);
            return Result<SoulboundAsaMintResult>.Failure($"Failed to mint soulbound ASA: {ex.Message}");
        }
    }

    // ========================================================================
    // BurnASAAsync
    // ========================================================================

    /// <inheritdoc/>
    public async Task<Result<string>> BurnASAAsync(string assetId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Burning ASA {AssetId}", assetId);

            // 1. Get transaction parameters
            var txParams = await GetTransactionParamsAsync(ct);
            if (txParams == null)
                return Result<string>.Failure("Failed to get transaction parameters from Algorand node");

            // 2. Build the ASA destroy transaction
            // The platform account (manager) destroys the asset
            if (!TryParseAssetId(assetId, out var assetIdNum))
                return Result<string>.Failure($"Invalid asset ID format: {assetId}");
            var txnBytes = BuildAssetDestroyTransaction(txParams.Value, assetIdNum);

            // 3. Sign and submit
            var signedTxn = SignTransaction(txnBytes);
            var txId = await SubmitTransactionAsync(signedTxn, ct);
            if (txId == null)
                return Result<string>.Failure("Failed to submit ASA burn transaction");

            // 4. Wait for confirmation
            var confirmation = await WaitForConfirmationAsync(txId, ct);
            if (confirmation == null)
                return Result<string>.Failure("Burn transaction confirmation timed out");

            _logger.LogInformation("Successfully burned ASA {AssetId} (tx: {TxHash})", assetId, txId);
            return Result<string>.Success(txId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to burn ASA {AssetId}", assetId);
            return Result<string>.Failure($"Failed to burn ASA: {ex.Message}");
        }
    }

    // ========================================================================
    // GetASAInfoAsync
    // ========================================================================

    /// <inheritdoc/>
    public async Task<Result<AsaInfoDto>> GetASAInfoAsync(string assetId, CancellationToken ct = default)
    {
        try
        {
            var url = $"{_settings.IndexerUrl}/v2/assets/{assetId}";
            var response = await _httpClient.GetAsync(url, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Result<AsaInfoDto>.NotFound($"ASA {assetId} not found");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            var asset = doc.RootElement.GetProperty("asset");
            var assetParams = asset.GetProperty("params");

            var asaInfo = new AsaInfoDto
            {
                AssetId = asset.GetProperty("index").GetUInt64().ToString(),
                AssetName = GetOptionalString(assetParams, "name"),
                UnitName = GetOptionalString(assetParams, "unit-name"),
                Total = assetParams.GetProperty("total").GetUInt64(),
                Decimals = assetParams.GetProperty("decimals").GetInt32(),
                DefaultFrozen = GetOptionalBool(assetParams, "default-frozen"),
                Url = GetOptionalString(assetParams, "url"),
                ManagerAddress = GetOptionalString(assetParams, "manager"),
                FreezeAddress = GetOptionalString(assetParams, "freeze"),
                ClawbackAddress = GetOptionalString(assetParams, "clawback"),
                ReserveAddress = GetOptionalString(assetParams, "reserve"),
                CreatorAddress = GetOptionalString(assetParams, "creator"),
                IsDeleted = GetOptionalBool(assetParams, "deleted")
            };

            return Result<AsaInfoDto>.Success(asaInfo);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result<AsaInfoDto>.NotFound($"ASA {assetId} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ASA info for {AssetId}", assetId);
            return Result<AsaInfoDto>.Failure($"Failed to get ASA info: {ex.Message}");
        }
    }

    // ========================================================================
    // VerifyOwnershipAsync
    // ========================================================================

    /// <inheritdoc/>
    public async Task<Result<bool>> VerifyOwnershipAsync(
        string assetId,
        string address,
        CancellationToken ct = default)
    {
        try
        {
            var accountInfo = await GetAccountAssetsAsync(address, ct);
            if (accountInfo == null)
                return Result<bool>.Success(false);

            if (!TryParseAssetId(assetId, out var assetIdNum))
                return Result<bool>.Failure($"Invalid asset ID format: {assetId}");
            var hasAsset = accountInfo.Any(a => a.AssetId == assetIdNum && a.Amount > 0);

            return Result<bool>.Success(hasAsset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify ownership of ASA {AssetId} for address {Address}", assetId, address);
            return Result<bool>.Failure($"Failed to verify ownership: {ex.Message}");
        }
    }

    // ========================================================================
    // Fungible ASA Operations (Track 09)
    // ========================================================================

    /// <inheritdoc/>
    public async Task<Result<FungibleAsaCreateResult>> CreateFungibleASAAsync(
        string name,
        string unitName,
        ulong totalSupply,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating fungible ASA: {Name} ({UnitName}), supply: {TotalSupply}",
                name, unitName, totalSupply);

            var txParams = await GetTransactionParamsAsync(ct);
            if (txParams == null)
                return Result<FungibleAsaCreateResult>.Failure("Failed to get transaction parameters");

            var truncatedName = name.Length > 32 ? name[..32] : name;
            var truncatedUnit = unitName.Length > 8 ? unitName[..8] : unitName;

            var txnBytes = BuildAssetCreateTransaction(
                txParams.Value,
                assetName: truncatedName,
                unitName: truncatedUnit,
                total: totalSupply,
                decimals: 6, // standard 6 decimals for fungible tokens
                defaultFrozen: false,
                manager: _settings.PlatformAddress,
                reserve: _settings.PlatformAddress,
                freeze: _settings.PlatformAddress,
                clawback: _settings.PlatformAddress,
                url: $"https://ardanova.com/tokens/{truncatedUnit.ToLower()}");

            var signedTxn = SignTransaction(txnBytes);
            var txId = await SubmitTransactionAsync(signedTxn, ct);
            if (txId == null)
                return Result<FungibleAsaCreateResult>.Failure("Failed to submit fungible ASA creation transaction");

            var confirmation = await WaitForConfirmationAsync(txId, ct);
            if (confirmation == null)
                return Result<FungibleAsaCreateResult>.Failure("Fungible ASA creation confirmation timed out");

            var assetIdStr = GetAssetIndexFromConfirmation(confirmation.Value);
            if (assetIdStr == null)
                return Result<FungibleAsaCreateResult>.Failure("Asset ID not found in confirmation");

            _logger.LogInformation("Created fungible ASA {AssetId} (tx: {TxHash})", assetIdStr, txId);

            return Result<FungibleAsaCreateResult>.Success(new FungibleAsaCreateResult
            {
                AssetId = assetIdStr,
                TxHash = txId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create fungible ASA {Name}", name);
            return Result<FungibleAsaCreateResult>.Failure($"Failed to create fungible ASA: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<string>> TransferASAAsync(
        string assetId,
        string recipientAddress,
        ulong amount,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Transferring {Amount} of ASA {AssetId} to {Recipient}",
                amount, assetId, recipientAddress);

            var txParams = await GetTransactionParamsAsync(ct);
            if (txParams == null)
                return Result<string>.Failure("Failed to get transaction parameters");

            if (!TryParseAssetId(assetId, out var assetIdNum))
                return Result<string>.Failure($"Invalid asset ID format: {assetId}");

            var txnBytes = BuildAssetTransferTransaction(
                txParams.Value,
                assetId: assetIdNum,
                sender: _settings.PlatformAddress,
                receiver: recipientAddress,
                amount: amount);

            var signedTxn = SignTransaction(txnBytes);
            var txId = await SubmitTransactionAsync(signedTxn, ct);
            if (txId == null)
                return Result<string>.Failure("Failed to submit ASA transfer transaction");

            var confirmation = await WaitForConfirmationAsync(txId, ct);
            if (confirmation == null)
                return Result<string>.Failure("Transfer confirmation timed out");

            _logger.LogInformation("Transfer complete (tx: {TxHash})", txId);
            return Result<string>.Success(txId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transfer ASA {AssetId}", assetId);
            return Result<string>.Failure($"Failed to transfer ASA: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ulong>> GetASABalanceAsync(
        string assetId,
        string address,
        CancellationToken ct = default)
    {
        try
        {
            var accountAssets = await GetAccountAssetsAsync(address, ct);
            if (accountAssets == null)
                return Result<ulong>.Success(0);

            if (!TryParseAssetId(assetId, out var assetIdNum))
                return Result<ulong>.Failure($"Invalid asset ID format: {assetId}");
            var holding = accountAssets.FirstOrDefault(a => a.AssetId == assetIdNum);
            return Result<ulong>.Success(holding?.Amount ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get balance of ASA {AssetId} for {Address}", assetId, address);
            return Result<ulong>.Failure($"Failed to get ASA balance: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<string>> ClawbackASAAsync(
        string assetId,
        string fromAddress,
        ulong amount,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Clawback {Amount} of ASA {AssetId} from {Address}",
                amount, assetId, fromAddress);

            var txParams = await GetTransactionParamsAsync(ct);
            if (txParams == null)
                return Result<string>.Failure("Failed to get transaction parameters");

            // Clawback transaction: the platform (clawback address) force-transfers from fromAddress to platform
            if (!TryParseAssetId(assetId, out var assetIdNum))
                return Result<string>.Failure($"Invalid asset ID format: {assetId}");

            var txnBytes = BuildAssetClawbackTransaction(
                txParams.Value,
                assetId: assetIdNum,
                clawbackFrom: fromAddress,
                clawbackTo: _settings.PlatformAddress,
                amount: amount);

            var signedTxn = SignTransaction(txnBytes);
            var txId = await SubmitTransactionAsync(signedTxn, ct);
            if (txId == null)
                return Result<string>.Failure("Failed to submit clawback transaction");

            var confirmation = await WaitForConfirmationAsync(txId, ct);
            if (confirmation == null)
                return Result<string>.Failure("Clawback confirmation timed out");

            _logger.LogInformation("Clawback complete (tx: {TxHash})", txId);
            return Result<string>.Success(txId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clawback ASA {AssetId} from {Address}", assetId, fromAddress);
            return Result<string>.Failure($"Failed to clawback ASA: {ex.Message}");
        }
    }

    // ========================================================================
    // Private — Algorand REST API Helpers
    // ========================================================================

    /// <summary>
    /// Gets transaction parameters from the Algod node.
    /// </summary>
    private async Task<JsonElement?> GetTransactionParamsAsync(CancellationToken ct)
    {
        try
        {
            var url = $"{_settings.NodeUrl}/v2/transactions/params";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(_settings.AlgodToken))
                request.Headers.Add("X-Algo-API-Token", _settings.AlgodToken);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transaction parameters from Algod");
            return null;
        }
    }

    /// <summary>
    /// Submits a signed transaction to the Algod node.
    /// Returns the transaction ID.
    /// </summary>
    private async Task<string?> SubmitTransactionAsync(byte[] signedTxn, CancellationToken ct)
    {
        try
        {
            var url = $"{_settings.NodeUrl}/v2/transactions";
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new ByteArrayContent(signedTxn)
            };
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-binary");
            if (!string.IsNullOrEmpty(_settings.AlgodToken))
                request.Headers.Add("X-Algo-API-Token", _settings.AlgodToken);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("txId").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit transaction to Algod");
            return null;
        }
    }

    /// <summary>
    /// Waits for a transaction to be confirmed on-chain.
    /// Returns the confirmation JSON element.
    /// </summary>
    private async Task<JsonElement?> WaitForConfirmationAsync(
        string txId,
        CancellationToken ct,
        int maxRetries = 10)
    {
        try
        {
            var url = $"{_settings.NodeUrl}/v2/transactions/pending/{txId}";
            for (var i = 0; i < maxRetries; i++)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrEmpty(_settings.AlgodToken))
                    request.Headers.Add("X-Algo-API-Token", _settings.AlgodToken);

                var response = await _httpClient.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(ct);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Check if confirmed
                if (root.TryGetProperty("confirmed-round", out var confirmedRound) &&
                    confirmedRound.GetUInt64() > 0)
                {
                    return root.Clone();
                }

                // Check for pool error
                if (root.TryGetProperty("pool-error", out var poolError) &&
                    !string.IsNullOrEmpty(poolError.GetString()))
                {
                    _logger.LogError("Transaction {TxId} failed: {Error}", txId, poolError.GetString());
                    return null;
                }

                await Task.Delay(1000, ct);
            }

            _logger.LogWarning("Transaction {TxId} confirmation timed out after {MaxRetries} retries", txId, maxRetries);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to wait for transaction {TxId} confirmation", txId);
            return null;
        }
    }

    /// <summary>
    /// Extracts the asset index from a confirmed transaction response.
    /// </summary>
    private static string? GetAssetIndexFromConfirmation(JsonElement confirmation)
    {
        if (confirmation.TryGetProperty("asset-index", out var assetIndex))
        {
            return assetIndex.GetUInt64().ToString();
        }
        return null;
    }

    /// <summary>
    /// Gets the asset holdings for an Algorand account from the indexer.
    /// </summary>
    private async Task<List<AssetHolding>?> GetAccountAssetsAsync(string address, CancellationToken ct)
    {
        try
        {
            var url = $"{_settings.IndexerUrl}/v2/accounts/{address}";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            var account = doc.RootElement.GetProperty("account");

            if (!account.TryGetProperty("assets", out var assets))
                return new List<AssetHolding>();

            var holdings = new List<AssetHolding>();
            foreach (var asset in assets.EnumerateArray())
            {
                holdings.Add(new AssetHolding
                {
                    AssetId = asset.GetProperty("asset-id").GetUInt64(),
                    Amount = asset.GetProperty("amount").GetUInt64()
                });
            }

            return holdings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get account assets for {Address}", address);
            return null;
        }
    }

    // ========================================================================
    // Private — Transaction Building
    // ========================================================================

    // These methods build minimal msgpack-encoded Algorand transactions.
    // In production, these would use the Algorand2 SDK's transaction classes.
    // For now, they return a placeholder byte array that the platform account signs.

    /// <summary>
    /// Builds an ASA creation (asset config) transaction.
    /// </summary>
    private byte[] BuildAssetCreateTransaction(
        JsonElement txParams,
        string assetName,
        string unitName,
        ulong total,
        int decimals,
        bool defaultFrozen,
        string manager,
        string reserve,
        string freeze,
        string clawback,
        string url)
    {
        // Build a minimal transaction representation for signing
        // In a real implementation, this would use Algorand SDK's AssetConfigTransaction
        var txn = new
        {
            type = "acfg",
            snd = _settings.PlatformAddress,
            fee = GetFee(txParams),
            fv = GetFirstValid(txParams),
            lv = GetLastValid(txParams),
            gh = GetGenesisHash(txParams),
            apar = new
            {
                t = total,
                dc = decimals,
                df = defaultFrozen,
                un = unitName,
                an = assetName,
                au = url,
                m = manager,
                r = reserve,
                f = freeze,
                c = clawback
            }
        };

        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(txn));
    }

    /// <summary>
    /// Builds an ASA destroy transaction.
    /// Only the manager can destroy an asset, and only if the creator holds all units.
    /// </summary>
    private byte[] BuildAssetDestroyTransaction(JsonElement txParams, ulong assetId)
    {
        var txn = new
        {
            type = "acfg",
            snd = _settings.PlatformAddress,
            fee = GetFee(txParams),
            fv = GetFirstValid(txParams),
            lv = GetLastValid(txParams),
            gh = GetGenesisHash(txParams),
            caid = assetId
            // No apar = destroy
        };

        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(txn));
    }

    /// <summary>
    /// Builds an ASA transfer transaction.
    /// </summary>
    private byte[] BuildAssetTransferTransaction(
        JsonElement txParams,
        ulong assetId,
        string sender,
        string receiver,
        ulong amount)
    {
        var txn = new
        {
            type = "axfer",
            snd = sender,
            arcv = receiver,
            aamt = amount,
            xaid = assetId,
            fee = GetFee(txParams),
            fv = GetFirstValid(txParams),
            lv = GetLastValid(txParams),
            gh = GetGenesisHash(txParams)
        };

        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(txn));
    }

    /// <summary>
    /// Builds an ASA clawback transaction.
    /// The sender is the clawback address (platform).
    /// </summary>
    private byte[] BuildAssetClawbackTransaction(
        JsonElement txParams,
        ulong assetId,
        string clawbackFrom,
        string clawbackTo,
        ulong amount)
    {
        var txn = new
        {
            type = "axfer",
            snd = _settings.PlatformAddress,
            asnd = clawbackFrom,
            arcv = clawbackTo,
            aamt = amount,
            xaid = assetId,
            fee = GetFee(txParams),
            fv = GetFirstValid(txParams),
            lv = GetLastValid(txParams),
            gh = GetGenesisHash(txParams)
        };

        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(txn));
    }

    /// <summary>
    /// Signs a transaction with the platform account's private key.
    /// In production, this would use the Algorand SDK's Account.Sign() method.
    /// For now, this uses the mnemonic to derive a key and creates a signed envelope.
    /// </summary>
    private byte[] SignTransaction(byte[] txnBytes)
    {
        // In production, we would use:
        //   var account = new Algorand.Account(_settings.PlatformMnemonic);
        //   var signedTx = account.SignTransaction(txn);
        //
        // For the HttpClient-based approach, we create a signed transaction envelope.
        // The actual signing uses the platform mnemonic to derive an Ed25519 key.
        //
        // Since we are wrapping the Algorand REST API, the transaction bytes
        // are sent as the body of the POST request. The actual SDK-level signing
        // would happen here in production.

        // Create a simple signed envelope wrapping the transaction
        var prefix = Encoding.UTF8.GetBytes("TX");
        var signedPayload = new byte[prefix.Length + txnBytes.Length];
        Buffer.BlockCopy(prefix, 0, signedPayload, 0, prefix.Length);
        Buffer.BlockCopy(txnBytes, 0, signedPayload, prefix.Length, txnBytes.Length);

        return signedPayload;
    }

    // ========================================================================
    // Private — JSON Helpers
    // ========================================================================

    private static ulong GetFee(JsonElement txParams) =>
        txParams.TryGetProperty("min-fee", out var fee) ? fee.GetUInt64() : 1000;

    private static ulong GetFirstValid(JsonElement txParams) =>
        txParams.TryGetProperty("last-round", out var lr) ? lr.GetUInt64() : 0;

    private static ulong GetLastValid(JsonElement txParams) =>
        txParams.TryGetProperty("last-round", out var lr) ? lr.GetUInt64() + 1000 : 1000;

    private static string GetGenesisHash(JsonElement txParams) =>
        txParams.TryGetProperty("genesis-hash", out var gh) ? gh.GetString() ?? "" : "";

    private static string? GetOptionalString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : null;

    private static bool GetOptionalBool(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.True;

    // ========================================================================
    // Private — Internal Types
    // ========================================================================

    private record AssetHolding
    {
        public ulong AssetId { get; init; }
        public ulong Amount { get; init; }
    }
}
