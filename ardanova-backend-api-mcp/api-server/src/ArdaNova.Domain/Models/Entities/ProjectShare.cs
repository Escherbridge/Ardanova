using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(projectId), IsUnique = true)]
[Table("ProjectShare")]
public class ProjectShare
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectId { get; set; } = string.Empty;

    public string? assetId { get; set; }

    [Required]
    public string name { get; set; } = string.Empty;

    [Required]
    public string symbol { get; set; } = string.Empty;

    [Required]
    [Precision(18, 8)]
    public decimal totalSupply { get; set; }

    [Required]
    public int decimals { get; set; }

    [Required]
    public string allocation { get; set; } = string.Empty;

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
    public virtual Project? Project { get; set; }

    public virtual ICollection<ShareHolder> ShareHolders { get; set; } = new List<ShareHolder>();

    public virtual ICollection<Fundraising> Fundraisings { get; set; } = new List<Fundraising>();

    public virtual ICollection<TaskEscrow> TaskEscrows { get; set; } = new List<TaskEscrow>();

    public virtual ICollection<ShareSwap> ShareSwapsAsFromShare { get; set; } = new List<ShareSwap>();

    public virtual ICollection<ShareSwap> ShareSwapsAsToShare { get; set; } = new List<ShareSwap>();

    public virtual ICollection<LiquidityPool> LiquidityPoolsAsShare1 { get; set; } = new List<LiquidityPool>();

    public virtual ICollection<LiquidityPool> LiquidityPoolsAsShare2 { get; set; } = new List<LiquidityPool>();

}
