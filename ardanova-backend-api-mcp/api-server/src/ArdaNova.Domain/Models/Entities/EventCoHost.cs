// Generated from database-architecture.dbml by generate:csharp - do not edit; hand-written members go in EventCoHost.Custom.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("EventCoHost")]
public partial class EventCoHost
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string eventId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public DateTime addedAt { get; set; }

    [ForeignKey("eventId")]
    public virtual Event? Event { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
