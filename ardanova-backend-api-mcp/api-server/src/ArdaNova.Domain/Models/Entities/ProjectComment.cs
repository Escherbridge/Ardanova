using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProjectComment")]
public class ProjectComment
{
    [Key]
    public string id { get; set; }

    [Required]
    public string projectId { get; set; }

    [Required]
    public string userId { get; set; }

    [Required]
    public string content { get; set; }

    public string? parentId { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("projectId")]
    public virtual Project Project { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

    [ForeignKey("parentId")]
    public virtual ProjectComment Parent { get; set; }

}
