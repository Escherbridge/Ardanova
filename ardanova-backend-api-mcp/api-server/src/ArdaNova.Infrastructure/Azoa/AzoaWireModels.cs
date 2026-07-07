namespace ArdaNova.Infrastructure.Azoa;

using System.Text.Json.Serialization;

/// <summary>
/// Wire shape of the AZOA node's envelope (<c>AZOAResult&lt;T&gt;</c>): the node
/// returns <c>{ isError, message, result }</c>. We deserialize into this and then
/// translate into ArdaNova's <c>Result&lt;T&gt;</c> at the client boundary.
/// </summary>
internal sealed class AzoaResultEnvelope<T>
{
    [JsonPropertyName("isError")]
    public bool IsError { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("result")]
    public T? Result { get; set; }
}

// ── Avatar onboarding (self-register; §4, §11.2) ──────────────────────────────

public sealed class AzoaAvatarRegisterRequest
{
    [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;
    [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
    [JsonPropertyName("password")] public string Password { get; set; } = string.Empty;
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("firstName")] public string? FirstName { get; set; }
    [JsonPropertyName("lastName")] public string? LastName { get; set; }
}

public sealed class AzoaAvatar
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("username")] public string? Username { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
}

// ── Allocation door (§6) ──────────────────────────────────────────────────────

public enum AzoaAllocationKind
{
    Mint = 0,
    Transfer = 1
}

public sealed class AzoaAllocationRequest
{
    [JsonPropertyName("kind")] public AzoaAllocationKind Kind { get; set; } = AzoaAllocationKind.Mint;
    [JsonPropertyName("chainType")] public string ChainType { get; set; } = string.Empty;

    /// <summary>Already-decided amount as an opaque string. AZOA derives no economics (§1, §3).</summary>
    [JsonPropertyName("amount")] public string Amount { get; set; } = string.Empty;

    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("assetId")] public string? AssetId { get; set; }

    /// <summary>Transfer only: the existing asset record to move.</summary>
    [JsonPropertyName("assetRecordId")] public Guid? AssetRecordId { get; set; }

    [JsonPropertyName("metadata")] public Dictionary<string, string> Metadata { get; set; } = new();
    [JsonPropertyName("memo")] public string? Memo { get; set; }
}

public sealed class AzoaAllocationResult
{
    [JsonPropertyName("avatarId")] public Guid AvatarId { get; set; }
    [JsonPropertyName("walletId")] public Guid WalletId { get; set; }
    [JsonPropertyName("walletAddress")] public string WalletAddress { get; set; } = string.Empty;
    [JsonPropertyName("walletProvisioned")] public bool WalletProvisioned { get; set; }
    [JsonPropertyName("operationId")] public Guid OperationId { get; set; }

    /// <summary>True when the idempotency key replayed an existing op — no second value move (§6, §7).</summary>
    [JsonPropertyName("replayed")] public bool Replayed { get; set; }
    [JsonPropertyName("idempotencyKey")] public string IdempotencyKey { get; set; } = string.Empty;
}

// ── Wallet generate (caller-self-scoped; pre-KYC OK, §11.4) ────────────────────

public sealed class AzoaWalletGenerateRequest
{
    [JsonPropertyName("chainType")] public string ChainType { get; set; } = string.Empty;
    [JsonPropertyName("label")] public string? Label { get; set; }
    [JsonPropertyName("isDefault")] public bool IsDefault { get; set; }
}

public sealed class AzoaWallet
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("address")] public string? Address { get; set; }
    [JsonPropertyName("chainType")] public string? ChainType { get; set; }
}
