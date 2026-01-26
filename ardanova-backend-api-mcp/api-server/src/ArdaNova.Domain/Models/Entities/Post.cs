using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Post")]
public class Post
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string authorId { get; set; } = string.Empty;

    public string? projectId { get; set; }

    public string? guildId { get; set; }

    public string? shopId { get; set; }

    [Required]
    public PostType type { get; set; }

    [Required]
    public PostVisibility visibility { get; set; }

    public string? title { get; set; }

    [Required]
    [Column(TypeName = "text")]
    public string content { get; set; } = string.Empty;

    public string? metadata { get; set; }

    [Required]
    public int likesCount { get; set; }

    [Required]
    public int commentsCount { get; set; }

    [Required]
    public int sharesCount { get; set; }

    [Required]
    public int viewsCount { get; set; }

    [Required]
    public bool isPinned { get; set; }

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

    [ForeignKey("authorId")]
    public virtual User? Author { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("guildId")]
    public virtual Guild? Guild { get; set; }

    [ForeignKey("shopId")]
    public virtual Shop? Shop { get; set; }

    public virtual ICollection<PostMedia> PostMedias { get; set; } = new List<PostMedia>();

    public virtual ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();

    public virtual ICollection<PostComment> PostComments { get; set; } = new List<PostComment>();

    public virtual ICollection<PostBookmark> PostBookmarks { get; set; } = new List<PostBookmark>();

    public virtual ICollection<PostShare> PostShares { get; set; } = new List<PostShare>();

}
