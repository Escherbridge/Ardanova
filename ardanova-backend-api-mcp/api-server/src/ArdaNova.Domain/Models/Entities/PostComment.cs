using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("PostComment")]
public class PostComment
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string postId { get; set; } = string.Empty;

    [Required]
    public string authorId { get; set; } = string.Empty;

    public string? parentId { get; set; }

    [Required]
    [Column(TypeName = "text")]
    public string content { get; set; } = string.Empty;

    [Required]
    public int likesCount { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("postId")]
    public virtual Post? Post { get; set; }

    [ForeignKey("authorId")]
    public virtual User? Author { get; set; }

    [ForeignKey("parentId")]
    public virtual PostComment? Parent { get; set; }

}
