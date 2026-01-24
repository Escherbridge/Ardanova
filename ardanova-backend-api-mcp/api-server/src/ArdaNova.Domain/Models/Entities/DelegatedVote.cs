using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("DelegatedVote")]
public class DelegatedVote
{
    [Key]
    public string id { get; set; }

    [Required]
    public string projectId { get; set; }

    [Required]
    public string delegatorId { get; set; }

    [Required]
    public string delegateeId { get; set; }

    [Required]
    public string tokenId { get; set; }

    [Required]
    public decimal amount { get; set; }

    [Required]
    public bool isActive { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    public DateTime? expiresAt { get; set; }

    public DateTime? revokedAt { get; set; }

    [ForeignKey("projectId")]
    public virtual Project Project { get; set; }

    [ForeignKey("delegatorId")]
    public virtual User Delegator { get; set; }

    [ForeignKey("delegateeId")]
    public virtual User Delegatee { get; set; }

    [ForeignKey("tokenId")]
    public virtual ProjectToken Token { get; set; }

}
