using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("EventReminder")]
public class EventReminder
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string eventId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public DateTime remindAt { get; set; }

    [Required]
    public bool isSent { get; set; }

    public DateTime? sentAt { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("eventId")]
    public virtual Event? Event { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
