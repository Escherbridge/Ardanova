using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProjectMembershipRequest")]
public class ProjectMembershipRequest
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string projectId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public ProjectRole requestedRole { get; set; }

    [Required]
    [Column(TypeName = "text")]
    public string message { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string? skills { get; set; }

    [Column(TypeName = "text")]
    public string? motivation { get; set; }

    [Column(TypeName = "text")]
    public string? portfolio { get; set; }

    [Required]
    public MembershipRequestStatus status { get; set; }

    public string? reviewedById { get; set; }

    [Column(TypeName = "text")]
    public string? reviewMessage { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    public DateTime? reviewedAt { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("userId")]
    [InverseProperty("ProjectMembershipRequestsAsUser")]
    public virtual User? User { get; set; }

    [ForeignKey("reviewedById")]
    [InverseProperty("ProjectMembershipRequestsAsReviewedBy")]
    public virtual User? ReviewedBy { get; set; }

}
