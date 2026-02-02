using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("PostShare")]
public class PostShare
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string postId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    public string? sharedToProjectId { get; set; }

    public string? sharedToGuildId { get; set; }

    [Column(TypeName = "text")]
    public string? comment { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("postId")]
    public virtual Post? Post { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

    [ForeignKey("sharedToProjectId")]
    public virtual Project? SharedToProject { get; set; }

    [ForeignKey("sharedToGuildId")]
    public virtual Guild? SharedToGuild { get; set; }

}
