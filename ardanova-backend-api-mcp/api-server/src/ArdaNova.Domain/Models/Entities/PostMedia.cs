using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("PostMedia")]
public class PostMedia
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string postId { get; set; } = string.Empty;

    [Required]
    public MimeType type { get; set; }

    [Required]
    public string url { get; set; } = string.Empty;

    public string? thumbnailUrl { get; set; }

    public string? altText { get; set; }

    [Required]
    public int order { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("postId")]
    public virtual Post? Post { get; set; }

}
