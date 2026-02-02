using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("GuildApplication")]
public class GuildApplication
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string guildId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public GuildMemberRole requestedRole { get; set; }

    [Required]
    [Column(TypeName = "text")]
    public string message { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string? skills { get; set; }

    [Column(TypeName = "text")]
    public string? experience { get; set; }

    [Column(TypeName = "text")]
    public string? portfolio { get; set; }

    public string? availability { get; set; }

    [Required]
    public MembershipRequestStatus status { get; set; }

    public string? reviewedById { get; set; }

    [Column(TypeName = "text")]
    public string? reviewMessage { get; set; }

    [Required]
    public DateTime appliedAt { get; set; }

    public DateTime? reviewedAt { get; set; }

    [ForeignKey("guildId")]
    public virtual Guild? Guild { get; set; }

    [ForeignKey("userId")]
    [InverseProperty("GuildApplicationsAsUser")]
    public virtual User? User { get; set; }

    [ForeignKey("reviewedById")]
    [InverseProperty("GuildApplicationsAsReviewedBy")]
    public virtual User? ReviewedBy { get; set; }

}
