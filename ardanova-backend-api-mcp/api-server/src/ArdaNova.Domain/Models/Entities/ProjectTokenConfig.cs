using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(projectId), IsUnique = true)]
[Table("ProjectTokenConfig")]
public class ProjectTokenConfig
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectId { get; set; } = string.Empty;

    public string? assetId { get; set; }

    [Required]
    public string assetName { get; set; } = string.Empty;

    [Required]
    public string unitName { get; set; } = string.Empty;

    public int? assetScale { get; set; }

    [Required]
    public int totalSupply { get; set; }

    [Required]
    public int allocatedSupply { get; set; }

    [Required]
    public int distributedSupply { get; set; }

    [Required]
    public int reservedSupply { get; set; }

    public string? mintTxHash { get; set; }

    [Required]
    public ProjectTokenStatus status { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    /// <summary>What the project is asking for, in US dollars - <c>ProjectFundingUnits.Ledger</c>.</summary>
    [Required]
    public double fundingGoal { get; set; }

    /// <summary>Dollars settled into the raise so far. Same unit as <see cref="fundingGoal"/>, by rule.</summary>
    [Required]
    public double fundingRaised { get; set; }

    [Required]
    public ProjectGateStatus gateStatus { get; set; }

    public DateTime? gate1ClearedAt { get; set; }

    public DateTime? gate2ClearedAt { get; set; }

    public DateTime? failedAt { get; set; }

    [Required]
    public int contributorSupply { get; set; }

    [Required]
    public int investorSupply { get; set; }

    [Required]
    public int founderSupply { get; set; }

    [Required]
    public int burnedSupply { get; set; }

    [Column(TypeName = "text")]
    public string? successCriteria { get; set; }

    public string? successVerifiedBy { get; set; }

    // ── Node-side provisioning (self-serve project token, W5.2) ─────────────
    // Three facts, each written only once its node call actually succeeded. The
    // fundability predicates below READ these facts; nothing stores a separate
    // "provisioned" flag that could disagree with them.
    // Rationale: src/ArdaNova.Domain/Models/Entities/AGENTS.md § project token.

    /// <summary>Chain the project asset was minted on; null until the mint succeeds.</summary>
    public string? assetChain { get; set; }

    /// <summary>Chain the project's node-side treasury account lives on.</summary>
    public string? treasuryChain { get; set; }

    /// <summary>Opaque project reference this tenant addresses the treasury by on the node.</summary>
    public string? treasuryReference { get; set; }

    /// <summary>Address of the project's node-side custodial treasury — where funding ARDA lands.</summary>
    public string? treasuryAddress { get; set; }

    /// <summary>When the node resolved (or created) the treasury account.</summary>
    public DateTime? treasuryResolvedAt { get; set; }

    /// <summary>Declared cap of the node reserve ledger's <c>contributor</c> partition, in whole units.</summary>
    [Required]
    public int contributorPartitionSupply { get; set; }

    /// <summary>Declared cap of the node reserve ledger's <c>investor</c> partition, in whole units.</summary>
    [Required]
    public int investorPartitionSupply { get; set; }

    /// <summary>When the node accepted the partition declaration; null means the node polices nothing yet.</summary>
    public DateTime? partitionsDeclaredAt { get; set; }

    /// <summary>Owner-authored disclosure shown before anyone commits funds (Q5 / Q-X3).</summary>
    [Column(TypeName = "text")]
    public string? unitsDisclosure { get; set; }

    /// <summary>When every provisioning leg completed.</summary>
    public DateTime? provisionedAt { get; set; }

    /// <summary>The project owner who ran provisioning; never a caller-supplied id.</summary>
    public string? provisionedByUserId { get; set; }

    /// <summary>Last provisioning failure, kept so the owner is told what to retry rather than "not configured".</summary>
    [Column(TypeName = "text")]
    public string? provisioningFailureReason { get; set; }

    /// <summary>
    /// True only when all three node legs are persisted: the minted asset, the treasury
    /// the project's funds land in, and the reserve partitions the node draws against.
    /// </summary>
    [NotMapped]
    public bool IsNodeProvisioned =>
        !string.IsNullOrWhiteSpace(assetId)
        && !string.IsNullOrWhiteSpace(treasuryAddress)
        && partitionsDeclaredAt.HasValue;

    /// <summary>The one fundability predicate every value path reads. A frozen or dissolved token is never fundable.</summary>
    [NotMapped]
    public bool IsFundable =>
        IsNodeProvisioned && status == ProjectTokenStatus.ACTIVE;

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    public virtual ICollection<TokenAllocation> TokenAllocations { get; set; } = new List<TokenAllocation>();

    public virtual ICollection<TokenBalance> TokenBalances { get; set; } = new List<TokenBalance>();

    public virtual ICollection<PayoutRequest> PayoutRequests { get; set; } = new List<PayoutRequest>();

    public virtual ICollection<ProjectInvestment> ProjectInvestments { get; set; } = new List<ProjectInvestment>();

    public virtual ICollection<FundingIntent> FundingIntents { get; set; } = new List<FundingIntent>();

    public virtual ICollection<TaskCommerceAgreement> TaskCommerceAgreements { get; set; } = new List<TaskCommerceAgreement>();

}
