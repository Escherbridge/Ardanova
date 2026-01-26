using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(token), IsUnique = true)]
[Table("ProjectInvitation")]
public class ProjectInvitation
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectId { get; set; } = string.Empty;

    [Required]
    public string invitedById { get; set; } = string.Empty;

    public string? invitedUserId { get; set; }

    public string? invitedEmail { get; set; }

    [Required]
    public ProjectRole role { get; set; }

    [Column(TypeName = "text")]
    public string? message { get; set; }

    [Required]
    public InvitationStatus status { get; set; }

    public string? token { get; set; }

    public DateTime? expiresAt { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    public DateTime? respondedAt { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("invitedById")]
    [InverseProperty("ProjectInvitationsAsInvitedBy")]
    public virtual User? InvitedBy { get; set; }

    [ForeignKey("invitedUserId")]
    [InverseProperty("ProjectInvitationsAsInvitedUser")]
    public virtual User? InvitedUser { get; set; }

}
