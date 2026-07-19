namespace ArdaNova.Infrastructure.Algorand;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;

/// <summary>Deterministic in-process implementation with no chain, HTTP, or signing dependency.</summary>
public sealed class SimulatedAlgorandService : IAlgorandService
{
    private readonly ConcurrentDictionary<string, AsaInfoDto> _assets = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<(string AssetId, string Address), ulong> _balances = new();

    public Task<Result<SoulboundAsaMintResult>> MintSoulboundASAAsync(
        string recipientAddress,
        CredentialMetadataInput metadata,
        CancellationToken ct = default)
    {
        var assetId = SimulatedId("credential-asset", metadata.CredentialId, recipientAddress);
        var txHash = SimulatedId("credential-mint", metadata.CredentialId, recipientAddress);
        _assets[assetId] = new AsaInfoDto
        {
            AssetId = assetId,
            AssetName = $"{metadata.ScopeName} Membership",
            UnitName = "CRED",
            Total = 1,
            Decimals = 0,
            DefaultFrozen = true,
        };
        _balances[(assetId, recipientAddress)] = 1;
        return Task.FromResult(Result<SoulboundAsaMintResult>.Success(new SoulboundAsaMintResult
        {
            AssetId = assetId,
            TxHash = txHash,
        }));
    }

    public Task<Result<string>> BurnASAAsync(string assetId, CancellationToken ct = default)
    {
        _assets.TryRemove(assetId, out _);
        foreach (var holding in _balances.Keys.Where(key => key.AssetId == assetId))
            _balances.TryRemove(holding, out _);
        return Task.FromResult(Result<string>.Success(SimulatedId("burn", assetId)));
    }

    public Task<Result<AsaInfoDto>> GetASAInfoAsync(string assetId, CancellationToken ct = default)
        => Task.FromResult(_assets.TryGetValue(assetId, out var asset)
            ? Result<AsaInfoDto>.Success(asset)
            : Result<AsaInfoDto>.NotFound($"Simulated asset {assetId} was not found."));

    public Task<Result<bool>> VerifyOwnershipAsync(
        string assetId,
        string address,
        CancellationToken ct = default)
        => Task.FromResult(Result<bool>.Success(
            _balances.TryGetValue((assetId, address), out var amount) && amount > 0));

    public Task<Result<string>> BuildARC19MetadataAsync(
        CredentialMetadataInput credential,
        CancellationToken ct = default)
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
                version = "1.0",
            },
        };
        return Task.FromResult(Result<string>.Success(JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        })));
    }

    public Task<Result<FungibleAsaCreateResult>> CreateFungibleASAAsync(
        string name,
        string unitName,
        ulong totalSupply,
        CancellationToken ct = default)
    {
        var assetId = SimulatedId("fungible-asset", name, unitName, totalSupply.ToString());
        var txHash = SimulatedId("fungible-mint", assetId);
        _assets[assetId] = new AsaInfoDto
        {
            AssetId = assetId,
            AssetName = name,
            UnitName = unitName,
            Total = totalSupply,
            Decimals = 0,
        };
        return Task.FromResult(Result<FungibleAsaCreateResult>.Success(new FungibleAsaCreateResult
        {
            AssetId = assetId,
            TxHash = txHash,
        }));
    }

    public Task<Result<string>> TransferASAAsync(
        string assetId,
        string recipientAddress,
        ulong amount,
        CancellationToken ct = default)
    {
        if (!_assets.ContainsKey(assetId))
            return Task.FromResult(Result<string>.NotFound($"Simulated asset {assetId} was not found."));
        _balances.AddOrUpdate((assetId, recipientAddress), amount, (_, current) => checked(current + amount));
        return Task.FromResult(Result<string>.Success(
            SimulatedId("transfer", assetId, recipientAddress, amount.ToString())));
    }

    public Task<Result<ulong>> GetASABalanceAsync(
        string assetId,
        string address,
        CancellationToken ct = default)
        => Task.FromResult(Result<ulong>.Success(
            _balances.TryGetValue((assetId, address), out var amount) ? amount : 0));

    public Task<Result<string>> ClawbackASAAsync(
        string assetId,
        string fromAddress,
        ulong amount,
        CancellationToken ct = default)
    {
        if (!_balances.TryGetValue((assetId, fromAddress), out var current) || current < amount)
            return Task.FromResult(Result<string>.Conflict("Simulated holding has insufficient balance."));
        _balances[(assetId, fromAddress)] = current - amount;
        return Task.FromResult(Result<string>.Success(
            SimulatedId("clawback", assetId, fromAddress, amount.ToString())));
    }

    private static string SimulatedId(string operation, params string[] values)
    {
        var payload = string.Join('\n', new[] { operation }.Concat(values));
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return $"sim:{operation}:{digest[..24]}";
    }
}
