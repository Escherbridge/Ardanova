using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProposalExecution")]
public class ProposalExecution
{
    [Key]
    public string id { get; set; }

    [Required]
    public string proposalId { get; set; }

    [Required]
    public DateTime executedAt { get; set; }

    public string? txHash { get; set; }

    public string? result { get; set; }

    [ForeignKey("proposalId")]
    public virtual Proposal Proposal { get; set; }

}
