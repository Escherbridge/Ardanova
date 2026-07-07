using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ArdaNova.Application.Azoa.Quests;

// ────────────────────────────────────────────────────────────────────────────
//  Core DAG types
//  Serialise to the AZOA node's quest-definition JSON shape (contract §5.2).
//  All property names use [JsonPropertyName] so camelCase wire format matches
//  the AZOA node's expected contract exactly.
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// A single node in an AZOA quest DAG (contract §5.2).
/// </summary>
public sealed class AzoaQuestNode
{
    /// <summary>Unique identifier within the DAG.</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Discriminator string matching AZOA node types:
    /// GateCheck | Emit | HolonCreate | Grant | Transfer | Refund | FungibleTokenCreate
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Opaque config object; concrete type corresponds to <see cref="Type"/>.
    /// Null for nodes that carry no configuration (e.g. bare Emit with empty payload).
    /// </summary>
    [JsonPropertyName("config")]
    public object? Config { get; init; }

    /// <summary>Edge targets — ordered list of node ids this node transitions to.</summary>
    [JsonPropertyName("next")]
    public List<string> Next { get; init; } = new();
}

/// <summary>
/// Root object published to the AZOA node as a quest definition (contract §5.2).
/// </summary>
public sealed class AzoaQuestDefinition
{
    /// <summary>Human-readable quest name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Optional description shown in the AZOA explorer.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    /// <summary>
    /// When true any avatar may instantiate this quest without a specific grant.
    /// </summary>
    [JsonPropertyName("isPublic")]
    public bool IsPublic { get; init; }

    /// <summary>Ordered list of DAG nodes. The first node is the entry point.</summary>
    [JsonPropertyName("nodes")]
    public List<AzoaQuestNode> Nodes { get; init; } = new();
}

// ────────────────────────────────────────────────────────────────────────────
//  Per-type config DTOs  (contract §5.2 — replicate exactly)
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Config for a <c>GateCheck</c> node.
/// Evaluates a boolean predicate against values read from the run context.
/// </summary>
public sealed class GateCheckNodeConfig
{
    /// <summary>Boolean expression evaluated at runtime (e.g. "fundingGoalMet == true").</summary>
    [JsonPropertyName("predicate")]
    public string Predicate { get; init; } = string.Empty;

    /// <summary>
    /// Named bindings the predicate may reference.
    /// Keys are the variable names used in <see cref="Predicate"/>;
    /// values are the context-path descriptors resolved by the AZOA runner.
    /// </summary>
    [JsonPropertyName("reads")]
    public Dictionary<string, object> Reads { get; init; } = new();
}

/// <summary>
/// Config for an <c>Emit</c> node.
/// The payload is an opaque JSON object consumed by downstream listeners.
/// </summary>
public sealed class EmitNodeConfig
{
    /// <summary>Arbitrary JSON payload forwarded verbatim to event consumers.</summary>
    [JsonPropertyName("payload")]
    public object Payload { get; init; } = new { };
}

/// <summary>
/// Config for a <c>Grant</c> node — mints an NFT and optionally associates it with a Holon.
/// The actor avatar is sourced from the run context, NOT the config body.
/// </summary>
public sealed class GrantNodeConfig
{
    /// <summary>NFT mint parameters (maps to AZOA NftMintRequest).</summary>
    [JsonPropertyName("request")]
    public object Request { get; init; } = new { };

    /// <summary>
    /// Optional Holon id the minted NFT is anchored to.
    /// Omitted when null so the wire format stays clean.
    /// </summary>
    [JsonPropertyName("holonId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HolonId { get; init; }
}

/// <summary>
/// Config for a <c>Transfer</c> node — transfers an existing NFT to the actor avatar.
/// </summary>
public sealed class TransferNodeConfig
{
    /// <summary>Identifier of the NFT to transfer.</summary>
    [JsonPropertyName("nftId")]
    public string NftId { get; init; } = string.Empty;

    /// <summary>Transfer parameters (maps to AZOA NftTransferRequest).</summary>
    [JsonPropertyName("request")]
    public object Request { get; init; } = new { };
}

/// <summary>
/// Config for a <c>Refund</c> node — returns an NFT to its original owner.
/// </summary>
public sealed class RefundNodeConfig
{
    /// <summary>Identifier of the NFT to refund.</summary>
    [JsonPropertyName("nftId")]
    public string NftId { get; init; } = string.Empty;

    /// <summary>Refund parameters (maps to AZOA NftTransferRequest).</summary>
    [JsonPropertyName("request")]
    public object Request { get; init; } = new { };
}

/// <summary>
/// Config for a <c>FungibleTokenCreate</c> node — mints a new fungible ASA on-chain.
/// </summary>
public sealed class FungibleTokenCreateNodeConfig
{
    /// <summary>Target chain. Currently only <c>"Algorand"</c> is supported.</summary>
    [JsonPropertyName("chainType")]
    public string ChainType { get; init; } = "Algorand";

    /// <summary>Full token name (e.g. "ArdaNova Project Share").</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Short ticker symbol shown on-chain (e.g. "ANPS").</summary>
    [JsonPropertyName("unitName")]
    public string UnitName { get; init; } = string.Empty;

    /// <summary>Total supply before decimal scaling.</summary>
    [JsonPropertyName("total")]
    public ulong Total { get; init; }

    /// <summary>Decimal places (0 = indivisible integer token).</summary>
    [JsonPropertyName("decimals")]
    public int Decimals { get; init; }

    /// <summary>
    /// Optional Holon id the created ASA is anchored to.
    /// Omitted when null so the wire format stays clean.
    /// </summary>
    [JsonPropertyName("holonId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HolonId { get; init; }
}
