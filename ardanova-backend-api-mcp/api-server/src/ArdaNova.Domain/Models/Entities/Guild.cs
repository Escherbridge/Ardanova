using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Guild")]
public class Guild
{
    [Key]
    public string id { get; set; }

    [Required]
    public string name { get; set; }

    [Required]
    public string slug { get; set; }

    [Required]
    public string description { get; set; }

    public string? website { get; set; }

    [Required]
    public string email { get; set; }

    public string? phone { get; set; }

    public string? address { get; set; }

    public string? logo { get; set; }

    public string? portfolio { get; set; }

    public string? specialties { get; set; }

    [Required]
    public bool isVerified { get; set; }

    public decimal? rating { get; set; }

    [Required]
    public int reviewsCount { get; set; }

    [Required]
    public int projectsCount { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [Required]
    public string ownerId { get; set; }

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    [ForeignKey("ownerId")]
    public virtual User Owner { get; set; }

    public virtual ICollection<GuildMember> GuildMembers { get; set; } = new List<GuildMember>();

    public virtual ICollection<ProjectBid> ProjectBids { get; set; } = new List<ProjectBid>();

    public virtual ICollection<GuildReview> GuildReviews { get; set; } = new List<GuildReview>();

}
