using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("TaskEscrow")]
public class TaskEscrow
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string taskId { get; set; } = string.Empty;

    [Required]
    public string funderId { get; set; } = string.Empty;

    [Required]
    public string tokenId { get; set; } = string.Empty;

    [Required]
    public decimal amount { get; set; }

    [Required]
    public EscrowStatus status { get; set; }

    public string? txHashFund { get; set; }

    public string? txHashRelease { get; set; }

    public string? txHashRefund { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    public DateTime? fundedAt { get; set; }

    public DateTime? releasedAt { get; set; }

    public DateTime? refundedAt { get; set; }

    [ForeignKey("taskId")]
    public virtual ProjectTask? Task { get; set; }

    [ForeignKey("funderId")]
    public virtual User? Funder { get; set; }

    [ForeignKey("tokenId")]
    public virtual ProjectToken? Token { get; set; }

}
