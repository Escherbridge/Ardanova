using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProjectToken")]
public class ProjectToken
{
    [Key]
    public string id { get; set; }

    [Required]
    public string projectId { get; set; }

    public string? assetId { get; set; }

    [Required]
    public string name { get; set; }

    [Required]
    public string symbol { get; set; }

    [Required]
    public decimal totalSupply { get; set; }

    [Required]
    public int decimals { get; set; }

    [Required]
    public string allocation { get; set; }

    public string? vestingConfig { get; set; }

    [Required]
    public bool isDeployed { get; set; }

    public DateTime? deployedAt { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    public virtual ICollection<DelegatedVote> DelegatedVotes { get; set; } = new List<DelegatedVote>();

    [ForeignKey("projectId")]
    public virtual Project Project { get; set; }

    public virtual ICollection<TokenHolder> TokenHolders { get; set; } = new List<TokenHolder>();

    public virtual ICollection<ICO> ICOs { get; set; } = new List<ICO>();

    public virtual ICollection<TaskEscrow> TaskEscrows { get; set; } = new List<TaskEscrow>();

    public virtual ICollection<TokenSwap> TokenSwaps { get; set; } = new List<TokenSwap>();

    public virtual ICollection<LiquidityPool> LiquidityPools { get; set; } = new List<LiquidityPool>();

}
