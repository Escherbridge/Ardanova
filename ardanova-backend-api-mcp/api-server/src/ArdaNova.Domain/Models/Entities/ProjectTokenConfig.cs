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

    [Required]
    public double fundingGoal { get; set; }

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

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    public virtual ICollection<TokenAllocation> TokenAllocations { get; set; } = new List<TokenAllocation>();

    public virtual ICollection<TokenBalance> TokenBalances { get; set; } = new List<TokenBalance>();

    public virtual ICollection<PayoutRequest> PayoutRequests { get; set; } = new List<PayoutRequest>();

    public virtual ICollection<ProjectInvestment> ProjectInvestments { get; set; } = new List<ProjectInvestment>();

}
