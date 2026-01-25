using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProjectBid")]
public class ProjectBid
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectId { get; set; } = string.Empty;

    [Required]
    public string guildId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public string proposal { get; set; } = string.Empty;

    public string? timeline { get; set; }

    [Required]
    public decimal budget { get; set; }

    public string? deliverables { get; set; }

    [Required]
    public BidStatus status { get; set; }

    [Required]
    public DateTime submittedAt { get; set; }

    public DateTime? reviewedAt { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("guildId")]
    public virtual Guild? Guild { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
