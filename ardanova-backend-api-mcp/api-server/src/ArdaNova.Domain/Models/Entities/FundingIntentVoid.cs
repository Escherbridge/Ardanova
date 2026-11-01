// Generated from database-architecture.dbml by generate:csharp - do not edit; hand-written members go in FundingIntentVoid.Custom.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("FundingIntentVoid")]
public partial class FundingIntentVoid
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string funderUserId { get; set; } = string.Empty;

    [Required]
    public string idempotencyKey { get; set; } = string.Empty;

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("funderUserId")]
    public virtual User? FunderUser { get; set; }

}
