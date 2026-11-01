// Generated from database-architecture.dbml by generate:csharp - do not edit; hand-written members go in ActorAssertionReplay.Custom.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ActorAssertionReplay")]
public partial class ActorAssertionReplay
{

    [Key]
    [Required]
    public string jti { get; set; } = string.Empty;

    [Required]
    public DateTime expiresAt { get; set; }

    [Required]
    public DateTime consumedAt { get; set; }

    [Required]
    public string subject { get; set; } = string.Empty;

    [Required]
    public string requestTarget { get; set; } = string.Empty;

    [Required]
    public string bodySha256 { get; set; } = string.Empty;

}
