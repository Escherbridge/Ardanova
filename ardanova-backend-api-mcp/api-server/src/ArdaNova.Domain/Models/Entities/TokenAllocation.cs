using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

/// <summary>
/// One commitment of a project's stake to a person or a backlog item, and — once the node has
/// actually drawn it — the read-model of the HOLD that backs it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The award of record is <see cref="tokenAmount"/></b>, a whole-unit count.
/// <see cref="equityPercentage"/> is a derived display share of the declared supply and is
/// never the thing anyone is paid; two award fields that can be written independently is how
/// a task's board figure and its agreement come to disagree.
/// </para>
/// <para>
/// <b>The hold columns are a mirror, not a source.</b> The node's hold ledger is authoritative
/// for whether units are committed, to whom and until when; these columns exist so ArdaNova can
/// show a person their own award without asking the node on every read. Nothing here may be
/// treated as proof a commitment exists — only a hold reference the node returned is written,
/// and <see cref="IsNodeHeld"/> is the single predicate every payout path reads.
/// </para>
/// </remarks>
[Index(nameof(holdId), IsUnique = true)]
[Index(nameof(workerHoldId), IsUnique = true)]
[Table("TokenAllocation")]
public class TokenAllocation
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectTokenConfigId { get; set; } = string.Empty;

    public string? pbiId { get; set; }

    public string? recipientUserId { get; set; }

    [Required]
    public double equityPercentage { get; set; }

    [Required]
    public int tokenAmount { get; set; }

    [Required]
    public AllocationStatus status { get; set; }

    [Required]
    public TokenHolderClass holderClass { get; set; }

    [Required]
    public bool isLiquid { get; set; }

    public DateTime? distributedAt { get; set; }

    public string? distributionTxHash { get; set; }

    public DateTime? burnedAt { get; set; }

    // -- Hold read-model (W7.2) -------------------------------------------------
    // Written only from what the node returned. A null here means the node holds
    // nothing for this allocation, which is why the payout paths refuse on it
    // rather than crediting a balance nothing backs.

    /// <summary>The commerce agreement this award was accepted under; null for a plain reservation.</summary>
    public string? taskCommerceAgreementId { get; set; }

    /// <summary>Rail the node's hold sits on; null until a hold exists.</summary>
    public string? holdChain { get; set; }

    /// <summary>Named reserve-ledger allowance the node drew this commitment from.</summary>
    public string? reservePartition { get; set; }

    /// <summary>The escrow hold: opened when the owner accepted the bid, released on approval.</summary>
    public string? holdId { get; set; }

    /// <summary>The node's last reported state for <see cref="holdId"/>.</summary>
    public string? holdState { get; set; }

    /// <summary>
    /// The worker's own window: the second hold the escrow hold is continued into when the
    /// owner approves completed work. It exists so the award is restricted until it is liquid,
    /// which defers the taxable event; it is NOT a chargeback window — reserve-sourced reward
    /// units were never bought with a card.
    /// </summary>
    public string? workerHoldId { get; set; }

    /// <summary>The node's last reported state for <see cref="workerHoldId"/>.</summary>
    public string? workerHoldState { get; set; }

    /// <summary>When the worker's window ends and the award can become liquid.</summary>
    public DateTime? workerHoldReleasesAt { get; set; }

    /// <summary>When the hold columns were last written from a node answer.</summary>
    public DateTime? holdSyncedAt { get; set; }

    /// <summary>True only when the node reported a live commitment for this allocation.</summary>
    [NotMapped]
    public bool IsNodeHeld => !string.IsNullOrWhiteSpace(holdId);

    /// <summary>True once the escrow hold has been continued into the worker's own window.</summary>
    [NotMapped]
    public bool IsAwaitingWorkerWindow => !string.IsNullOrWhiteSpace(workerHoldId);

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("projectTokenConfigId")]
    public virtual ProjectTokenConfig? ProjectTokenConfig { get; set; }

    [ForeignKey("pbiId")]
    public virtual ProductBacklogItem? Pbi { get; set; }

    [ForeignKey("recipientUserId")]
    public virtual User? RecipientUser { get; set; }

}
