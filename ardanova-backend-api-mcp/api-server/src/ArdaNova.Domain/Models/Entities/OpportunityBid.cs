using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("OpportunityBid")]
public class OpportunityBid
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string opportunityId { get; set; } = string.Empty;

    [Required]
    public string bidderId { get; set; } = string.Empty;

    public string? guildId { get; set; }

    [Precision(18, 8)]
    public decimal? proposedAmount { get; set; }

    [Required]
    [Column(TypeName = "text")]
    public string proposal { get; set; } = string.Empty;

    public int? estimatedHours { get; set; }

    [Column(TypeName = "text")]
    public string? timeline { get; set; }

    [Column(TypeName = "text")]
    public string? deliverables { get; set; }

    [Required]
    public BidStatus status { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    public DateTime? reviewedAt { get; set; }

    [ForeignKey("opportunityId")]
    public virtual Opportunity? Opportunity { get; set; }

    [ForeignKey("bidderId")]
    public virtual User? Bidder { get; set; }

    [ForeignKey("guildId")]
    public virtual Guild? Guild { get; set; }

}
