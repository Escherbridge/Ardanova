using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("PostBookmark")]
public class PostBookmark
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string postId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("postId")]
    public virtual Post? Post { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
