using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

/// <summary>A treasury RECEIPT: what ArdaNova recorded about a move the node actually made.</summary>
/// <remarks>
/// Not a ledger of record. The balance of record is the project's custodial treasury account
/// on the node; these rows explain that balance, they do not constitute it. A row with no
/// <c>azoaOperationId</c> attests to nothing on chain - it records ArdaNova's own policy split.
/// </remarks>
[Table("PlatformTreasuryTransaction")]
public class PlatformTreasuryTransaction
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public PlatformTreasuryTransactionType type { get; set; }

    /// <summary>Display projection of <see cref="amountMinorUnits"/>. Never an input to money math.</summary>
    [Required]
    public double amount { get; set; }

    /// <summary>The exact amount, in minor units (cents). This is the authoritative figure.</summary>
    [Required]
    public long amountMinorUnits { get; set; }

    public string? fromBucket { get; set; }

    public string? toBucket { get; set; }

    public string? relatedProjectId { get; set; }

    public string? relatedPayoutRequestId { get; set; }

    public string? description { get; set; }

    /// <summary>Display projection of <see cref="balanceAfterMinorUnits"/>. Never an input to money math.</summary>
    [Required]
    public double balanceAfter { get; set; }

    /// <summary>
    /// The policy-view balance after this receipt, in minor units. Null when the row records a
    /// move whose resulting balance only the node knows — a receipt must never invent one.
    /// </summary>
    public long? balanceAfterMinorUnits { get; set; }

    // -- AZOA provenance (W5.3) --------------------------------------------
    // The op id is what makes a receipt verifiable. A row with no azoaOperationId
    // records ArdaNova's own policy split, not a value move on the node.

    /// <summary>Durable node operation this receipt is for.</summary>
    public string? azoaOperationId { get; set; }

    /// <summary>The stable key the node deduped the move on; the reconcile handle.</summary>
    public string? azoaIdempotencyKey { get; set; }

    /// <summary>Opaque node receipt reference, safe to log and echo back.</summary>
    public string? azoaReceiptReference { get; set; }

    /// <summary>The on-chain asset the move was counted in.</summary>
    public string? chainAssetId { get; set; }

    /// <summary>Address of the project's node-side treasury this receipt credits.</summary>
    public string? projectTreasuryAddress { get; set; }

    /// <summary>True only when this row points at a node operation that can be reconciled.</summary>
    [NotMapped]
    public bool IsChainReceipt => !string.IsNullOrWhiteSpace(azoaOperationId);

    [Required]
    public DateTime createdAt { get; set; }

}
