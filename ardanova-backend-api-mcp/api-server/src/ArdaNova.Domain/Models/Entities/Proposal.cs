using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Proposal")]
public class Proposal
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectId { get; set; } = string.Empty;

    [Required]
    public string creatorId { get; set; } = string.Empty;

    [Required]
    public ProposalType type { get; set; }

    [Required]
    public string title { get; set; } = string.Empty;

    [Required]
    public string description { get; set; } = string.Empty;

    [Required]
    public string options { get; set; } = string.Empty;

    [Required]
    public int quorum { get; set; }

    [Required]
    public int threshold { get; set; }

    [Required]
    public ProposalStatus status { get; set; }

    public DateTime? votingStart { get; set; }

    public DateTime? votingEnd { get; set; }

    public int? executionDelay { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("creatorId")]
    public virtual User? Creator { get; set; }

    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();

    public virtual ICollection<ProposalExecution> ProposalExecutions { get; set; } = new List<ProposalExecution>();

}
