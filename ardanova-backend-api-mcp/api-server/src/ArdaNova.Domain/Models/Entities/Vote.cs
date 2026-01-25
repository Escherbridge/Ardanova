using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Vote")]
public class Vote
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string proposalId { get; set; } = string.Empty;

    [Required]
    public string voterId { get; set; } = string.Empty;

    [Required]
    public int choice { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal weight { get; set; }

    [Column(TypeName = "text")]
    public string? reason { get; set; }

    public string? txHash { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("proposalId")]
    public virtual Proposal? Proposal { get; set; }

    [ForeignKey("voterId")]
    public virtual User? Voter { get; set; }

}
