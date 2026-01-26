using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(token), IsUnique = true)]
[Table("GuildInvitation")]
public class GuildInvitation
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string guildId { get; set; } = string.Empty;

    [Required]
    public string invitedById { get; set; } = string.Empty;

    public string? invitedUserId { get; set; }

    public string? invitedEmail { get; set; }

    [Required]
    public GuildMemberRole role { get; set; }

    [Column(TypeName = "text")]
    public string? message { get; set; }

    [Required]
    public InvitationStatus status { get; set; }

    public string? token { get; set; }

    public DateTime? expiresAt { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    public DateTime? respondedAt { get; set; }

    [ForeignKey("guildId")]
    public virtual Guild? Guild { get; set; }

    [ForeignKey("invitedById")]
    [InverseProperty("GuildInvitationsAsInvitedBy")]
    public virtual User? InvitedBy { get; set; }

    [ForeignKey("invitedUserId")]
    [InverseProperty("GuildInvitationsAsInvitedUser")]
    public virtual User? InvitedUser { get; set; }

}
