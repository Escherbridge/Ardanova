using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Vote")]
public class Vote
{
    [Key]
    public string id { get; set; }

    [Required]
    public string proposalId { get; set; }

    [Required]
    public string voterId { get; set; }

    [Required]
    public int choice { get; set; }

    [Required]
    public decimal weight { get; set; }

    public string? reason { get; set; }

    public string? txHash { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("proposalId")]
    public virtual Proposal Proposal { get; set; }

    [ForeignKey("voterId")]
    public virtual User Voter { get; set; }

}
