using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("TaskBid")]
public class TaskBid
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string taskId { get; set; } = string.Empty;

    [Required]
    public string guildId { get; set; } = string.Empty;

    [Precision(18, 8)]
    public decimal? proposedAmount { get; set; }

    [Column(TypeName = "text")]
    public string? proposal { get; set; }

    public int? estimatedHours { get; set; }

    [Required]
    public TaskBidStatus status { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    public DateTime? reviewedAt { get; set; }

    [ForeignKey("taskId")]
    public virtual ProjectTask? Task { get; set; }

    [ForeignKey("guildId")]
    public virtual Guild? Guild { get; set; }

}
