// Generated from database-architecture.dbml by generate:csharp - do not edit; hand-written members go in UserSkill.Custom.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("UserSkill")]
public partial class UserSkill
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public string skill { get; set; } = string.Empty;

    [Required]
    public int level { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
