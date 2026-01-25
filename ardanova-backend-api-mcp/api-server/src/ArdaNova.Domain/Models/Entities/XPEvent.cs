using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("XPEvent")]
public class XPEvent
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public XPEventType eventType { get; set; }

    [Required]
    public int amount { get; set; }

    [Required]
    public string source { get; set; } = string.Empty;

    public string? sourceId { get; set; }

    public string? metadata { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
