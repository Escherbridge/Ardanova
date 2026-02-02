using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(slug), IsUnique = true)]
[Index(nameof(ownerId), IsUnique = true)]
[Table("Guild")]
public class Guild
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string name { get; set; } = string.Empty;

    [Required]
    public string slug { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "text")]
    public string description { get; set; } = string.Empty;

    public string? website { get; set; }

    [Required]
    public string email { get; set; } = string.Empty;

    public string? phone { get; set; }

    [Column(TypeName = "text")]
    public string? address { get; set; }

    public string? logo { get; set; }

    [Column(TypeName = "text")]
    public string? portfolio { get; set; }

    [Column(TypeName = "text")]
    public string? specialties { get; set; }

    [Required]
    public bool isVerified { get; set; }

    [Precision(18, 8)]
    public decimal? rating { get; set; }

    [Required]
    public int reviewsCount { get; set; }

    [Required]
    public int projectsCount { get; set; }

    [Required]
    public int membersCount { get; set; }

    [Required]
    public bool isTrending { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal trendingScore { get; set; }

    public int? trendingRank { get; set; }

    public DateTime? trendingAt { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [Required]
    public string ownerId { get; set; } = string.Empty;

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    [ForeignKey("ownerId")]
    public virtual User? Owner { get; set; }

    public virtual ICollection<GuildMember> GuildMembers { get; set; } = new List<GuildMember>();

    public virtual ICollection<GuildInvitation> GuildInvitations { get; set; } = new List<GuildInvitation>();

    public virtual ICollection<GuildApplication> GuildApplications { get; set; } = new List<GuildApplication>();

    public virtual ICollection<GuildReview> GuildReviews { get; set; } = new List<GuildReview>();

    public virtual ICollection<GuildUpdate> GuildUpdates { get; set; } = new List<GuildUpdate>();

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    public virtual ICollection<PostShare> PostShares { get; set; } = new List<PostShare>();

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual ICollection<GuildFollow> GuildFollows { get; set; } = new List<GuildFollow>();

    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();

    public virtual ICollection<OpportunityBid> OpportunityBids { get; set; } = new List<OpportunityBid>();

}
