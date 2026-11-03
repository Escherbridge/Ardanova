// Generated from database-architecture.dbml by generate:csharp - do not edit; hand-written members go in PayoutSaleVoid.Custom.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("PayoutSaleVoid")]
public partial class PayoutSaleVoid
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public string requestKey { get; set; } = string.Empty;

    [Required]
    public string quoteId { get; set; } = string.Empty;

    [Required]
    public DateTime createdAt { get; set; }

}
