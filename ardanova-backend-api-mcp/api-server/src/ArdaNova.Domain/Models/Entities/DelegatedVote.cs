using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("DelegatedVote")]
public class DelegatedVote
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectId { get; set; } = string.Empty;

    [Required]
    public string delegatorId { get; set; } = string.Empty;

    [Required]
    public string delegateeId { get; set; } = string.Empty;

    [Required]
    public string shareId { get; set; } = string.Empty;

    [Required]
    [Precision(18, 8)]
    public decimal amount { get; set; }

    [Required]
    public bool isActive { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    public DateTime? expiresAt { get; set; }

    public DateTime? revokedAt { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("delegatorId")]
    [InverseProperty("DelegatedVotesAsDelegator")]
    public virtual User? Delegator { get; set; }

    [ForeignKey("delegateeId")]
    [InverseProperty("DelegatedVotesAsDelegatee")]
    public virtual User? Delegatee { get; set; }

    [ForeignKey("shareId")]
    public virtual ProjectShare? Share { get; set; }

}
