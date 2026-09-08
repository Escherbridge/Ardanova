using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

/// <summary>
/// One "sell my units" request, and the read-model of the node exchange that settled it.
/// </summary>
/// <remarks>
/// <para>
/// <b>This row is a receipt, never the balance of record.</b> The units left and the platform
/// units arrived on the shared node; only what the node returned is written here, so a null
/// column means "the node has not told us yet" - never "zero" and never "done".
/// </para>
/// <para>
/// <b>It records the SELL leg only.</b> Turning platform units into money is a separate,
/// node-owned cash-out with its own durable ledger, read back through the payout service rather
/// than copied into Postgres: duplicating it would make two records of one movement of real
/// money, which is exactly the ledger-of-record this platform rejected for stake.
/// </para>
/// <para>
/// Reasoning: <c>src/ArdaNova.Application/Services/Implementations/AGENTS.md</c> section cash out.
/// </para>
/// </remarks>
[Table("PayoutRequest")]
public class PayoutRequest
{
    /// <summary>Prefix of the exchange idempotency key every unit sale is submitted under.</summary>
    /// <remarks>
    /// The key is DERIVED from <see cref="id"/> rather than stored, so the request that submits a
    /// sale and the reconciliation that chases it can never address two different node
    /// operations. See <see cref="ExchangeIdempotencyKey"/>.
    /// </remarks>
    public const string ExchangeIdempotencyKeyPrefix = "ardanova:unit-sale:";

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    public string? sourceProjectTokenConfigId { get; set; }

    [Required]
    public int sourceTokenAmount { get; set; }

    /// <summary>Whole platform units received, floored. A label; never money math.</summary>
    public int? ardaTokenAmount { get; set; }

    /// <summary>Dollar reference at the declared rate, for display. Never read back into money math.</summary>
    public double? usdAmount { get; set; }

    /// <summary>Platform units received, in exact base units at the platform's fixed scale.</summary>
    /// <remarks>
    /// The figure of record for what this sale paid. <see cref="ardaTokenAmount"/> and
    /// <see cref="usdAmount"/> beside it are floored display projections that predate fixed-scale
    /// money; nothing rounds here, so a cent cannot go missing.
    /// </remarks>
    public long? ardaMinorUnits { get; set; }

    /// <summary>The dollar reference the node stamped on the receipt, in exact minor units.</summary>
    public long? usdReferenceMinorUnits { get; set; }

    [Required]
    public PayoutStatus status { get; set; }

    [Required]
    public TokenHolderClass holderClass { get; set; }

    [Required]
    public ProjectGateStatus gateStatusAtRequest { get; set; }

    /// <summary>
    /// The node's receipt reference for the sale. Column name predates the exchange and is kept
    /// only so the stored schema does not move; read it through <see cref="SellReceiptReference"/>.
    /// </summary>
    public string? conversionTxHash { get; set; }

    /// <summary>
    /// The node's exchange operation id for the sale. Column name predates the exchange; read it
    /// through <see cref="SellOperationId"/>.
    /// </summary>
    public string? payoutTxHash { get; set; }

    /// <summary>
    /// RETIRED. Nothing has written this since the payout provider moved onto the shared node,
    /// which owns the cash-out record. Kept only until the column can be dropped.
    /// </summary>
    public string? stripePayoutId { get; set; }

    public string? failureReason { get; set; }

    [Required]
    public DateTime requestedAt { get; set; }

    public DateTime? processedAt { get; set; }

    public DateTime? completedAt { get; set; }

    /// <summary>The node's receipt reference for this sale, under its honest name.</summary>
    [NotMapped]
    public string? SellReceiptReference
    {
        get => conversionTxHash;
        set => conversionTxHash = value;
    }

    /// <summary>The node's exchange operation id for this sale, under its honest name.</summary>
    [NotMapped]
    public string? SellOperationId
    {
        get => payoutTxHash;
        set => payoutTxHash = value;
    }

    /// <summary>The one key this sale is submitted and reconciled under, derived from <see cref="id"/>.</summary>
    /// <remarks>
    /// Derived rather than stored on purpose: a stored key can be missing on a row whose
    /// submission already happened, and a reconciliation that cannot name the operation it is
    /// chasing has to guess. A GUID row id makes the derived key globally unique on its own.
    /// </remarks>
    [NotMapped]
    public string ExchangeIdempotencyKey => ExchangeIdempotencyKeyPrefix + id;

    /// <summary>True only once the node has returned a receipt for this sale.</summary>
    [NotMapped]
    public bool IsNodeSettled => !string.IsNullOrWhiteSpace(conversionTxHash);

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

    [ForeignKey("sourceProjectTokenConfigId")]
    public virtual ProjectTokenConfig? SourceProjectTokenConfig { get; set; }

}
