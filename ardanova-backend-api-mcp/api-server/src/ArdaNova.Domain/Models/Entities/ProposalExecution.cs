// Generated from database-architecture.dbml by generate:csharp - do not edit; hand-written members go in ProposalExecution.Custom.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(proposalId), IsUnique = true)]
[Table("ProposalExecution")]
public partial class ProposalExecution
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string proposalId { get; set; } = string.Empty;

    [Required]
    public DateTime executedAt { get; set; }

    public string? txHash { get; set; }

    [Column(TypeName = "jsonb")]
    public string? result { get; set; }

    [ForeignKey("proposalId")]
    public virtual Proposal? Proposal { get; set; }

}
