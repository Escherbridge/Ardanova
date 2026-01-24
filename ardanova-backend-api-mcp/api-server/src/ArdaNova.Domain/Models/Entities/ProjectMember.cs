using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProjectMember")]
public class ProjectMember
{
    [Key]
    public string id { get; set; }

    [Required]
    public string projectId { get; set; }

    [Required]
    public string userId { get; set; }

    [Required]
    public ProjectRole role { get; set; }

    [Required]
    public decimal tokenBalance { get; set; }

    [Required]
    public decimal votingPower { get; set; }

    [Required]
    public DateTime joinedAt { get; set; }

    public string? invitedById { get; set; }

    [ForeignKey("projectId")]
    public virtual Project Project { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

}
